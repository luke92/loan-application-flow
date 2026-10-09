import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { LoanApplicationForm } from "@/components/loan-application-form";
import { translations } from "@/lib/i18n/translations";

const push = jest.fn();
jest.mock("next/navigation", () => ({ useRouter: () => ({ push }) }));

const t = translations.loanApplicationForm;
const fetchMock = jest.fn();

function mockApiResponse(body: unknown) {
  fetchMock.mockResolvedValue({ ok: true, json: async () => body });
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>, ssn = "123456789") {
  await user.type(screen.getByLabelText(t.fields.firstName), "Jane");
  await user.type(screen.getByLabelText(t.fields.lastName), "Doe");
  await user.type(screen.getByLabelText(t.fields.street), "1 Main St");
  await user.type(screen.getByLabelText(t.fields.city), "Springfield");
  await user.selectOptions(screen.getByLabelText(t.fields.state), "CA");
  await user.type(screen.getByLabelText(t.fields.zip), "12345");
  await user.type(screen.getByLabelText(t.fields.companyName), "Acme Inc");
  await user.type(screen.getByLabelText(t.fields.requestedAmount), "1000");
  await user.type(screen.getByLabelText(t.fields.ssn), ssn);
}

beforeEach(() => {
  push.mockReset();
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

describe("LoanApplicationForm", () => {
  it("formats the SSN with dashes while typing", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);
    const ssn = screen.getByLabelText(t.fields.ssn);

    await user.type(ssn, "123456789");

    expect(ssn).toHaveValue("123-45-6789");
  });

  it("flags an SSN starting with 9 immediately, and clears it when changed", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);
    const ssn = screen.getByLabelText(t.fields.ssn);

    await user.type(ssn, "9");
    expect(screen.getByText(t.validation.ssnItin)).toBeInTheDocument();

    await user.clear(ssn);
    await user.type(ssn, "1");
    expect(screen.queryByText(t.validation.ssnItin)).not.toBeInTheDocument();
  });

  it("only accepts digits in the ZIP and adds the dash after 5", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);
    const zip = screen.getByLabelText(t.fields.zip);

    await user.type(zip, "ab12345cd6789");

    expect(zip).toHaveValue("12345-6789");
  });

  it("shows validation errors and does not call the API when the form is empty", async () => {
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await user.click(screen.getByRole("button", { name: t.submit }));

    expect(screen.getByText(t.validation.firstNameRequired)).toBeInTheDocument();
    expect(screen.getByText(t.validation.ssnRequired)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("sends the SSN without dashes and redirects to the success page when approved", async () => {
    mockApiResponse({ status: "Approved", applicationId: "abc-123", reason: null });
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await fillValidForm(user);
    expect(screen.getByLabelText(t.fields.ssn)).toHaveValue("123-45-6789");
    await user.click(screen.getByRole("button", { name: t.submit }));

    await waitFor(() => expect(push).toHaveBeenCalled());
    const [, init] = fetchMock.mock.calls[0];
    expect(JSON.parse(init.body)).toMatchObject({ ssn: "123456789", state: "CA", zip: "12345" });
    expect(push.mock.calls[0][0]).toMatch(/^\/success\?.*applicationId=abc-123/);
  });

  it("redirects to the denied page with the reason when denied", async () => {
    mockApiResponse({
      status: "Denied",
      applicationId: null,
      reason: "Applications from New York are not eligible.",
    });
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: t.submit }));

    await waitFor(() => expect(push).toHaveBeenCalled());
    const url = new URL(push.mock.calls[0][0], "http://localhost");
    expect(url.pathname).toBe("/denied");
    expect(url.searchParams.get("reason")).toBe("Applications from New York are not eligible.");
  });

  it("shows an error message when the request fails", async () => {
    fetchMock.mockRejectedValue(new Error("network"));
    const user = userEvent.setup();
    render(<LoanApplicationForm />);

    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: t.submit }));

    expect(await screen.findByText(t.submitError)).toBeInTheDocument();
    expect(push).not.toHaveBeenCalled();
  });
});
