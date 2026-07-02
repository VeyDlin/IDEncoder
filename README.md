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
- **Short output** — fixed 11 characters with the default Blowfish codec (vs 20 digits for `long.MaxValue`); 2–12 characters with the Sqids-style codec
- **Reversible** — decode back to the original number with the same key
- **Salted encoding** — same ID produces different strings for different entity types
- **Zero external crypto dependencies** — ships its own Blowfish implementation

## Install

```bash
dotnet add package IDEncoder                # core: encoding, EncodedId, JSON, DI
dotnet add package IDEncoder.AspNetCore     # + MVC model binding and JSON wiring
```

The core package has no ASP.NET Core dependency — it works in console apps, workers and
plain `dotnet/runtime` containers. Add `IDEncoder.AspNetCore` only in web apps.

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

## Codecs

`IDEncoder` is a facade over an `IIdCodec`. Two are built in:

- **`BlowfishIdCodec`** (default) — Blowfish encryption, fixed 11-char output, hides sequential IDs. `new IDEncoder("secret")` uses this.
- **`SqidsIdCodec`** — simple, non-secure, Sqids-style: short variable-length strings where sequential IDs look dissimilar, with no secret key. Obfuscation only, not cryptography.

```csharp
services.AddIDEncoder(new SqidsIdCodec());          // simple codec
services.AddIDEncoder(new BlowfishIdCodec("key"));  // explicit Blowfish
```

### Custom codecs

Implement `IIdCodec`, or derive from `IdCodec` to get salt caching for free, and register it:

```csharp
public sealed class MyCodec : IdCodec {
    public override int MaxEncodedLength => 16;
    public override string Encode(long id) => /* ... */;
    public override int Encode(long id, Span<char> destination) => /* ... */;
    public override long Decode(ReadOnlySpan<char> encoded) => /* ... */;
    protected override IIdCodec CreateWithSalt(string salt) => /* salted variation from base */;
}

services.AddIDEncoder(new MyCodec());
```

> Codec output is not self-describing. Don't switch codecs over data that already has encoded IDs in the wild — a string from one codec won't decode correctly with another.

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

Then wire MVC (IDEncoder.AspNetCore package):

```csharp
services.AddControllers(o => o.UseIDEncoderModelBinding())  // EncodedId route binding + [Salt]
    .AddIDEncoderJson();                                    // EncodedId JSON + [Salt] via DI
```

`AddIDEncoderJson()` binds JSON serialization to the encoder registered in DI (any of the
three registration styles above, including the deferred provider). The manual
`AddJsonOptions(o => o.JsonSerializerOptions.UseIDEncoderSalts())` setup still works and
uses the process-wide ambient encoder instead.

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

Salt support in JSON and route binding is enabled by the standard wiring:

```csharp
services.AddControllers(o => o.UseIDEncoderModelBinding())
    .AddIDEncoderJson();
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
| `Read(42)` | rejected with a 400 by default |

Raw JSON numbers are **rejected by default**: if they were accepted, any endpoint binding an
`EncodedId` from a request body would let clients address resources by plain sequential
numbers (`{"id": 1}`, `{"id": 2}`, …), bypassing the encoding entirely. If you are migrating
clients that still send numbers, enable it explicitly and consciously:

```csharp
.AddIDEncoderJson(allowNumericInput: true)
// or: o.JsonSerializerOptions.UseIDEncoder(encoder, allowNumericInput: true)
```

## Security notes

- **Encoding hides IDs; it does not authorize.** Nearly any well-formed 11-character Base62
  string decodes to *some* `long` — decode success does not prove the caller was ever given
  that ID. Always perform existence and authorization checks after decoding.
- Blowfish encoding is deterministic: the same ID with the same key and salt always produces
  the same string. Use salts to stop IDs from being tried across entity types.
- `SqidsIdCodec` is obfuscation only — anyone can decode its output without a key.

## Zero-alloc API

For high-throughput scenarios, span-based overloads avoid heap allocations:

```csharp
// Encode into a stack buffer sized for the active codec
Span<char> buffer = stackalloc char[encoder.MaxEncodedLength];
int n = encoder.Encode(42, buffer);

// Decode from the written slice (works for fixed- and variable-length codecs)
long id = encoder.Decode(buffer[..n]);
```

## Upgrading

### From 0.4.x to 0.5.0

- **Package split** — model binding (`UseIDEncoderModelBinding`) moved to the new
  `IDEncoder.AspNetCore` package; the core package no longer references ASP.NET Core.
  Web apps: add `IDEncoder.AspNetCore`. Everything stays in the `IDEncoder` namespace,
  so no `using` changes.
- **Raw JSON numbers are now rejected by default** (see JSON behavior above).
  Opt back in with `allowNumericInput: true` while migrating clients.
- **Malformed encoded IDs in JSON bodies now produce HTTP 400** (a `JsonException`)
  instead of a 500 (`ArgumentException`).
- `IDEncoder.EncodedLength` is obsolete — use `MaxEncodedLength` (codec-aware) or
  `BlowfishIdCodec.EncodedLength`.
- New: `AddIDEncoderJson()` (IDEncoder.AspNetCore) binds MVC JSON to the DI-registered
  encoder; `JsonSerializerOptions.UseIDEncoder(encoder)` binds standalone options to a
  specific encoder without touching process-wide state.

### From 0.3.x

The zero-alloc `Encode(long, Span<char>, …)` overload returns `int` (characters written)
instead of `void` since 0.4.0 — recompile and use the count to slice variable-length
output: `encoder.Decode(buffer[..n])`.
