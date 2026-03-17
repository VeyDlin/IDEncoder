namespace IDEncoder;


/// <summary>
/// Provides deferred initialization of <see cref="IDEncoder"/> for scenarios
/// where the secret key is not available at startup (e.g. loaded from a database at runtime).
/// Register with <see cref="ServiceCollectionExtensions.AddIDEncoderProvider"/>
/// and call <see cref="Configure"/> when the key becomes available.
/// </summary>
public sealed class IDEncoderProvider {
    private IDEncoder? encoder;

    /// <summary>
    /// Returns the configured <see cref="IDEncoder"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Configure"/> has not been called yet.
    /// </exception>
    public IDEncoder Encoder => encoder
        ?? throw new InvalidOperationException("IDEncoder is not configured yet. Call IDEncoderProvider.Configure() first.");


    /// <summary>
    /// Whether the encoder has been initialized via <see cref="Configure"/>.
    /// </summary>
    public bool IsConfigured => encoder is not null;


    /// <summary>
    /// Initializes the encoder with the given secret key.
    /// Also wires up <see cref="EncodedIdConverter"/> so JSON serialization
    /// and ASP.NET route binding start working immediately.
    /// Can only be called once per application lifetime.
    /// </summary>
    /// <param name="secretKey">
    /// Secret key for Blowfish encryption (1–56 bytes in UTF-8). Keys longer than 56 bytes are truncated.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Configure"/> has already been called.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretKey"/> is null or empty.
    /// </exception>
    public void Configure(string secretKey) {
        if (encoder is not null) {
            throw new InvalidOperationException("IDEncoder is already configured.");
        }

        var instance = new IDEncoder(secretKey);
        encoder = instance;
        EncodedIdConverter.Encoder = instance;
    }
}
