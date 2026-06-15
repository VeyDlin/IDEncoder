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


    /// <summary>Initializes the encoder with a Blowfish key. Can only be called once.</summary>
    /// <exception cref="InvalidOperationException">Thrown when already configured.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretKey"/> is null or empty.</exception>
    public void Configure(string secretKey) {
        Configure(new BlowfishIdCodec(secretKey));
    }


    /// <summary>Initializes the encoder with the given codec. Can only be called once.</summary>
    /// <exception cref="InvalidOperationException">Thrown when already configured.</exception>
    public void Configure(IIdCodec codec) {
        if (encoder is not null) {
            throw new InvalidOperationException("IDEncoder is already configured.");
        }

        var instance = new IDEncoder(codec);
        encoder = instance;
        EncodedIdConverter.Encoder = instance;
    }
}
