import { validateForm, type LoanApplicationFormData } from "@/lib/validation";
import { translations } from "@/lib/i18n/translations";

const messages = translations.loanApplicationForm.validation;

const valid: LoanApplicationFormData = {
  firstName: "Jane",
  lastName: "Doe",
  street: "1 Main St",
  city: "Springfield",
  state: "CA",
  zip: "12345",
  companyName: "Acme Inc",
  requestedAmount: 1000,
  ssn: "123-45-6789",
};

const errorsFor = (overrides: Partial<LoanApplicationFormData>) =>
  validateForm({ ...valid, ...overrides });

describe("validateForm", () => {
  it("accepts a valid application", () => {
    expect(validateForm(valid)).toEqual({});
  });

  it("requires the text fields", () => {
    const errors = validateForm({
      ...valid,
      firstName: " ",
      lastName: "",
      street: "",
      city: "",
      companyName: "",
    });
    expect(errors).toEqual({
      firstName: messages.firstNameRequired,
      lastName: messages.lastNameRequired,
      street: messages.streetRequired,
      city: messages.cityRequired,
      companyName: messages.companyNameRequired,
    });
  });

  it("requires a positive amount", () => {
    expect(errorsFor({ requestedAmount: 0 }).requestedAmount).toBe(messages.requestedAmountInvalid);
    expect(errorsFor({ requestedAmount: -5 }).requestedAmount).toBe(messages.requestedAmountInvalid);
  });

  describe("state", () => {
    it("is required", () => {
      expect(errorsFor({ state: "" }).state).toBe(messages.stateRequired);
    });
    it("must be 2 letters", () => {
      expect(errorsFor({ state: "C" }).state).toBe(messages.stateInvalid);
      expect(errorsFor({ state: "CAL" }).state).toBe(messages.stateInvalid);
    });
  });

  describe("zip", () => {
    it.each(["12345", "12345-6789"])("accepts %s", (zip) => {
      expect(errorsFor({ zip }).zip).toBeUndefined();
    });
    it.each(["1234", "123456", "12345-678", "abcde"])("rejects %s", (zip) => {
      expect(errorsFor({ zip }).zip).toBe(messages.zipInvalid);
    });
    it("is required", () => {
      expect(errorsFor({ zip: "" }).zip).toBe(messages.zipRequired);
    });
  });

  describe("ssn", () => {
    it("is required", () => {
      expect(errorsFor({ ssn: "" }).ssn).toBe(messages.ssnRequired);
    });
    it.each(["123456789", "123-45-678", "12-345-6789"])("rejects bad format %s", (ssn) => {
      expect(errorsFor({ ssn }).ssn).toBe(messages.ssnInvalid);
    });
    it("rejects an SSN starting with 9 (ITIN range)", () => {
      expect(errorsFor({ ssn: "923-45-6789" }).ssn).toBe(messages.ssnItin);
    });
    it("reports the format error first when both apply", () => {
      expect(errorsFor({ ssn: "9234" }).ssn).toBe(messages.ssnInvalid);
    });
  });
});
