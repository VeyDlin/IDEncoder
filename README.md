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
- **Salted encoding** — same ID produces different strings for different entity types
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

## Salt

By default, the same numeric ID always encodes to the same string. This means a user who knows a video ID could try it as a gallery ID, a user ID, etc.

Salt solves this — the same number produces different strings for different entity types:

```csharp
encoder.Encode(42);                // "xK9mQ3bPl2a"
encoder.Encode(42, "video");       // "t7RqN1cWm5z"
encoder.Encode(42, "gallery");     // "p3KmW8vLn9a"

// Decoding requires the same salt
encoder.Decode("t7RqN1cWm5z", "video");    // 42
encoder.Decode("p3KmW8vLn9a", "gallery");  // 42
```

Salted ciphers are cached internally — no performance penalty for reusing the same salt.

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

### Salt via attribute

Use `[Salt("...")]` on properties to automatically apply per-entity-type salt during JSON serialization:

```csharp
public record VideoResult(
    [property: Salt("video")] EncodedId Id,
    string Title
);

public record GalleryResult(
    [property: Salt("gallery")] EncodedId Id,
    string Title
);

// Same DB id = 42, but different JSON output:
// VideoResult:   { "id": "t7RqN1cWm5z", "title": "..." }
// GalleryResult: { "id": "p3KmW8vLn9a", "title": "..." }
```

Enable salt support in JSON and route binding:

```csharp
services.AddControllers(o => o.UseIDEncoderModelBinding())
    .AddJsonOptions(o => o.JsonSerializerOptions.UseIDEncoderSalts());
```

Salt also works on controller parameters for route/query binding:

```csharp
[HttpPost("{id}/test")]
public async Task<IActionResult> TestConnection([Salt("s3")] EncodedId id) {
    long dbId = id; // correctly decoded with "s3" salt
}
```

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

## License

MIT
