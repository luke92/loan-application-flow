import { expect, test, type Page } from "@playwright/test";

const API_URL = "**/api/applications";
const cors = {
  "access-control-allow-origin": "*",
  "access-control-allow-headers": "*",
  "access-control-allow-methods": "POST, OPTIONS",
};

// The backend is mocked at the network layer, so these run with only the frontend up.
async function mockApi(page: Page, body: object) {
  const requests: unknown[] = [];
  await page.route(API_URL, async (route) => {
    const request = route.request();
    if (request.method() === "OPTIONS") {
      return route.fulfill({ status: 204, headers: cors });
    }
    requests.push(request.postDataJSON());
    return route.fulfill({
      status: 200,
      headers: cors,
      contentType: "application/json",
      body: JSON.stringify(body),
    });
  });
  return requests;
}

async function fillForm(page: Page, overrides: { state?: string; ssn?: string } = {}) {
  await page.getByLabel("First name").fill("Jane");
  await page.getByLabel("Last name").fill("Doe");
  await page.getByLabel("Street").fill("1 Main St");
  await page.getByLabel("City").fill("Springfield");
  await page.getByLabel("State").selectOption(overrides.state ?? "CA");
  await page.getByLabel("ZIP").fill("12345");
  await page.getByLabel("Company name").fill("Acme Inc");
  await page.getByLabel("Requested amount").fill("1000");
  await page.getByLabel("SSN").pressSequentially(overrides.ssn ?? "123456789");
}

test.beforeEach(async ({ page }) => {
  await page.goto("/");
});

test("approved application shows the success page and sends a digits-only SSN", async ({ page }) => {
  const requests = await mockApi(page, { status: "Approved", applicationId: "abc-123", reason: null });

  await fillForm(page);
  await expect(page.getByLabel("SSN")).toHaveValue("123-45-6789");
  await page.getByRole("button", { name: "Submit application" }).click();

  await expect(page).toHaveURL(/\/success/);
  await expect(page.getByRole("heading", { name: "Application approved" })).toBeVisible();
  await expect(page.getByText("abc-123")).toBeVisible();
  expect(requests[0]).toMatchObject({ ssn: "123456789", state: "CA", zip: "12345" });
});

test("denied application shows the reason from the API", async ({ page }) => {
  await mockApi(page, {
    status: "Denied",
    applicationId: null,
    reason: "Applications from New York are not eligible.",
  });

  await fillForm(page, { state: "NY" });
  await page.getByRole("button", { name: "Submit application" }).click();

  await expect(page).toHaveURL(/\/denied/);
  await expect(page.getByText("Applications from New York are not eligible.")).toBeVisible();
});

test("SSN starting with 9 is flagged as soon as it is typed", async ({ page }) => {
  await page.getByLabel("SSN").pressSequentially("9");
  await expect(page.getByText("SSNs cannot start with 9")).toBeVisible();

  await page.getByLabel("SSN").fill("");
  await page.getByLabel("SSN").pressSequentially("1");
  await expect(page.getByText("SSNs cannot start with 9")).toBeHidden();
});

test("ZIP field ignores letters and adds the dash after 5 digits", async ({ page }) => {
  await page.getByLabel("ZIP").pressSequentially("ab12345cd6789");
  await expect(page.getByLabel("ZIP")).toHaveValue("12345-6789");
});

test("submitting an empty form shows validation errors and does not call the API", async ({ page }) => {
  const requests = await mockApi(page, { status: "Approved", applicationId: "x", reason: null });

  await page.getByRole("button", { name: "Submit application" }).click();

  await expect(page.getByText("First name is required.")).toBeVisible();
  await expect(page.getByText("SSN is required.")).toBeVisible();
  expect(requests).toHaveLength(0);
});

test("shows a connection error when the API is unreachable", async ({ page }) => {
  await page.route(API_URL, (route) => route.abort());

  await fillForm(page);
  await page.getByRole("button", { name: "Submit application" }).click();

  await expect(page.getByText("We couldn't submit your application")).toBeVisible();
});
