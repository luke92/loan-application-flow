import { formatSSN, formatZip } from "@/lib/format";

describe("formatSSN", () => {
  it.each([
    ["", ""],
    ["1", "1"],
    ["123", "123"],
    ["1234", "123-4"],
    ["12345", "123-45"],
    ["123456", "123-45-6"],
    ["123456789", "123-45-6789"],
    ["123-45-6789", "123-45-6789"],
    ["1234567890123", "123-45-6789"],
    ["12a-3b4", "123-4"],
  ])("formats %j as %j", (input, expected) => {
    expect(formatSSN(input)).toBe(expected);
  });
});

describe("formatZip", () => {
  it.each([
    ["", ""],
    ["12345", "12345"],
    ["123456", "12345-6"],
    ["123456789", "12345-6789"],
    ["12345-6789", "12345-6789"],
    ["1234567890", "12345-6789"],
    ["abc", ""],
    ["12a45", "1245"],
    ["12345-", "12345"],
  ])("formats %j as %j", (input, expected) => {
    expect(formatZip(input)).toBe(expected);
  });
});
