# IDEncoder

[![NuGet](https://img.shields.io/nuget/v/IDEncoder.svg)](https://www.nuget.org/packages/IDEncoder/)
[![License](https://img.shields.io/github/license/VeyDlin/IDEncoder)](LICENSE)

A .NET library that encodes numeric IDs (`long`) into short, URL-safe Base62 strings — similar to how YouTube, Twitter and other services expose public IDs like `dQw4w9WgXcQ` instead of raw database numbers.

Uses Blowfish encryption under the hood with no external cryptography dependencies — the cipher is implemented from scratch.

```
Database:  42
URL:       /video/xK9mQ3bPl2a
JSON:      { "id": "xK9mQ3bPl2a", "title": "..." }
```

## Why

- **Hide sequential IDs** — users can't guess or enumerate resource URLs
- **Short output** — always exactly 11 characters (vs 20 digits for `long.MaxValue`)
- **Reversible** — decode back to the original number with the same key
- **Zero external crypto dependencies** — ships its own Blowfish implementation

## Install

```bash
dotnet add package IDEncoder
```

## Quick start

```csharp
var encoder = new IDEncoder.IDEncoder("my-secret-key");

string encoded = encoder.Encode(42);       // "xK9mQ3bPl2a"
long decoded = encoder.Decode(encoded);    // 42
```

## ASP.NET Core integration

### Registration

```csharp
// Key known at startup
services.AddIDEncoder("my-secret-key");

// Key from configuration
services.AddIDEncoder(provider => {
    var config = provider.GetRequiredService<IConfiguration>();
    return config["IDEncoder:SecretKey"]!;
});

// Key loaded later (e.g. from database after startup)
services.AddIDEncoderProvider();
```

For deferred initialization, call `Configure` when the key becomes available:

```csharp
public class AppInitializer(IDEncoderProvider encoderProvider, IMyConfigService config) {
    public async Task InitializeAsync() {
        string secret = await config.GetSecretAsync("ID_ENCODER_SECRET");
        encoderProvider.Configure(secret);
    }
}
```

### EncodedId struct

A `long` wrapper that automatically encodes/decodes at JSON and route-binding boundaries:

```csharp
// DTO — JSON output is a Base62 string
public record ChatResult(EncodedId Id, string Title);

// Mapping — implicit conversion from long
return new ChatResult(Id: chat.Id, Title: chat.Title);
// { "id": "xK9mQ3bPl2a", "title": "My chat" }

// Controller — parsed from URL automatically via IParsable
[HttpGet("{id}")]
public async Task<IActionResult> Get(EncodedId id) {
    long dbId = id; // implicit conversion to long
    var chat = await db.Chats.FindAsync(dbId);
    return Ok(new ChatResult(chat.Id, chat.Title));
}
```

The database stores a plain `long`. Encoding only happens at the JSON/URL boundary.

### JSON behavior

| Input | Output |
|---|---|
| `Write(42)` | `"xK9mQ3bPl2a"` |
| `Read("xK9mQ3bPl2a")` | `42` |
| `Read(42)` | `42` (raw numbers accepted too) |

## Zero-alloc API

For high-throughput scenarios, span-based overloads avoid heap allocations:

```csharp
// Encode into a stack buffer
Span<char> buffer = stackalloc char[IDEncoder.IDEncoder.EncodedLength];
encoder.Encode(42, buffer);

// Decode from a span (no string needed)
long id = encoder.Decode(buffer);
```
