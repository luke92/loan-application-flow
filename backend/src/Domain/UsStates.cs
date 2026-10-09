namespace Domain;

public static class UsStates
{
    private static readonly IReadOnlyDictionary<string, string> Names =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = "Alabama",
            ["AK"] = "Alaska",
            ["AZ"] = "Arizona",
            ["AR"] = "Arkansas",
            ["CA"] = "California",
            ["CO"] = "Colorado",
            ["CT"] = "Connecticut",
            ["DE"] = "Delaware",
            ["DC"] = "District of Columbia",
            ["FL"] = "Florida",
            ["GA"] = "Georgia",
            ["HI"] = "Hawaii",
            ["ID"] = "Idaho",
            ["IL"] = "Illinois",
            ["IN"] = "Indiana",
            ["IA"] = "Iowa",
            ["KS"] = "Kansas",
            ["KY"] = "Kentucky",
            ["LA"] = "Louisiana",
            ["ME"] = "Maine",
            ["MD"] = "Maryland",
            ["MA"] = "Massachusetts",
            ["MI"] = "Michigan",
            ["MN"] = "Minnesota",
            ["MS"] = "Mississippi",
            ["MO"] = "Missouri",
            ["MT"] = "Montana",
            ["NE"] = "Nebraska",
            ["NV"] = "Nevada",
            ["NH"] = "New Hampshire",
            ["NJ"] = "New Jersey",
            ["NM"] = "New Mexico",
            ["NY"] = "New York",
            ["NC"] = "North Carolina",
            ["ND"] = "North Dakota",
            ["OH"] = "Ohio",
            ["OK"] = "Oklahoma",
            ["OR"] = "Oregon",
            ["PA"] = "Pennsylvania",
            ["RI"] = "Rhode Island",
            ["SC"] = "South Carolina",
            ["SD"] = "South Dakota",
            ["TN"] = "Tennessee",
            ["TX"] = "Texas",
            ["UT"] = "Utah",
            ["VT"] = "Vermont",
            ["VA"] = "Virginia",
            ["WA"] = "Washington",
            ["WV"] = "West Virginia",
            ["WI"] = "Wisconsin",
            ["WY"] = "Wyoming"
        };

    public static bool IsValid(string? code) => code is not null && Names.ContainsKey(code);

    /// <summary>Full state name for a 2-letter code, or the code itself (upper-cased) if unknown.</summary>
    public static string NameOf(string code) =>
        Names.TryGetValue(code, out var name) ? name : code.ToUpperInvariant();
}
