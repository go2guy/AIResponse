# Testing an AI response in C#

A small .NET 10 console example with 12 automated checks, no NuGet packages and no API key. Requires a .NET 10 SDK or later with the .NET 10 runtime.

Run from this directory:

```shell
dotnet run
```

Test an actual model response saved in a text file:

```shell
dotnet run -- response.json
```

Suggested prompt:

> What is the capital of France? Return only JSON with a string `answer` and a numeric `confidence` between 0 and 1.

Example response:

```json
{"answer":"Paris","confidence":0.99}
```

The validator checks JSON syntax, required fields, field types, confidence bounds, and a small set of accepted factual answers. Tests include both valid outputs and deliberately invalid outputs. A PASS for a negative case means the validator correctly rejected it. The process exits with code 1 on any unexpected result or invalid supplied response.

For a real AI integration, pass the returned model text to `ResponseValidator.Validate(responseText)` after your existing provider call. This example does not call a model. Keep these deterministic checks separate from live evaluations: run a collection of prompts repeatedly to measure a model's quality across varying responses.

The accepted-answer list is intentionally narrow and will reject some correct paraphrases. For open-ended responses, use a task-specific rubric, reviewed examples, and semantic evaluation. A model's self-reported confidence does not establish correctness. These checks also do not measure safety, latency, or overall model quality.

JSON parsing uses the built-in [System.Text.Json API](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument.parse).
