using System;
using System.Collections.Generic;
using System.Text.Json;

// Prompt: "What is the capital of France? Return only JSON with a string
// 'answer' and a numeric 'confidence' between 0 and 1."
// Test meaning and structure instead of exact wording, which AI can vary.
var cases = new (string Name, string Response, bool ShouldPass)[]
{
    ("Correct response", """{"answer":"Paris","confidence":0.99}""", true),
    ("Different wording", """{"answer":"The capital of France is Paris.","confidence":0.95}""", true),
    ("Wrong answer", """{"answer":"London","confidence":0.99}""", false),
    ("Misleading keyword match", """{"answer":"Paris is not the capital; London is.","confidence":0.99}""", false),
    ("Invalid JSON", "The capital is Paris.", false),
    ("Missing answer", """{"confidence":0.9}""", false),
    ("Empty answer", """{"answer":"","confidence":0.9}""", false),
    ("Wrong field type", """{"answer":42,"confidence":0.9}""", false),
    ("Missing confidence", """{"answer":"Paris"}""", false),
    ("Confidence out of range", """{"answer":"Paris","confidence":1.5}""", false),
    ("Wrong root type", "[]", false),
    ("Markdown wrapping", "```json\n{\"answer\":\"Paris\",\"confidence\":0.9}\n```", false)
};

var failures = 0;
foreach (var test in cases)
{
    var errors = ResponseValidator.Validate(test.Response);
    var passed = (errors.Count == 0) == test.ShouldPass;
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")}: {test.Name}");
    if (!passed)
    {
        failures++;
        Console.WriteLine($"  Expected valid={test.ShouldPass}. Errors: {string.Join("; ", errors)}");
    }
}

// Optional: validate actual model output saved as a UTF-8 text file.
// Supply only the model's response text, not the provider's HTTP envelope.
if (args.Length > 0)
{
    var errors = ResponseValidator.Validate(await File.ReadAllTextAsync(args[0]));
    Console.WriteLine(errors.Count == 0
        ? "PASS: Supplied AI response"
        : $"FAIL: Supplied AI response: {string.Join("; ", errors)}");
    if (errors.Count > 0) failures++;
}

Console.WriteLine($"\n{cases.Length} example tests completed; {failures} failure(s).");
return failures == 0 ? 0 : 1;

public static class ResponseValidator
{
    public static List<string> Validate(string response)
    {
        var errors = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(response);
        }
        catch (JsonException)
        {
            return ["Response must be valid JSON without markdown or extra text."];
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return ["Response must be a JSON object."];

            if (!root.TryGetProperty("answer", out var answer) ||
                answer.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(answer.GetString()))
            {
                errors.Add("'answer' must be a nonempty string.");
            }
            else
            {
                // A deliberately narrow acceptance set for this factual example.
                // Keyword-only checks would incorrectly accept 'Paris is not the capital'.
                var normalized = answer.GetString()!.Trim().TrimEnd('.').Trim();
                string[] accepted = ["Paris", "The capital of France is Paris", "Paris is the capital of France"];
                if (!accepted.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    errors.Add("Answer is outside the accepted correct answers for this example.");
            }

            if (!root.TryGetProperty("confidence", out var confidence) ||
                confidence.ValueKind != JsonValueKind.Number ||
                !confidence.TryGetDouble(out var value) ||
                !double.IsFinite(value) || value < 0 || value > 1)
            {
                errors.Add("'confidence' must be a number between 0 and 1.");
            }
        }
        return errors;
    }
}
