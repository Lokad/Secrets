using System;

namespace Lokad.Secrets
{
    /// <summary>
    ///     The result of resolving a secret. In addition to the secret value, also includes
    ///     the key that was used to resolve it, and additional information to better identify
    ///     how the secret was resolved.
    /// </summary>
    /// <remarks>
    ///     Serializing this object as JSON, or using <see cref="ToString"/>, will not expose the
    ///     secret value. 
    /// </remarks>
    [Newtonsoft.Json.JsonConverter(typeof(SecretJsonConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(SecretTextJsonConverter))]
    public struct SecretString
    {
        public SecretString(string key, string value, SecretSource source = SecretSource.Verbatim, string identity = "")
        {
            Key = key;
            Value = value;
            Source = source;
            Identity = identity;
        }

        /// <summary> The original value from the configuration file. </summary>
        /// <remarks> Should NOT be secret ! </remarks>
        public string Key { get; }

        /// <summary> The associated secret value.  </summary>
        public string Value { get; }

        /// <summary> Source of the secret value.  </summary>
        public SecretSource Source { get; }

        /// <summary> Additional information: date for the source file or version for the vault </summary>
        public string Identity { get; }

        /// <summary>
        ///     Safe ToString() implementation avoids leaking the secret value.
        /// </summary>
        public override string ToString() =>
            Source == SecretSource.Verbatim ? Key : $"{Key} {Source} {Identity}";

        /// <summary> Serializes a <see cref="SecretString"/> as a JSON string. </summary>
        /// <remarks>
        ///     Only supports writing (used to avoid serializing the secret itself to JSON, only serializes
        ///     the key).
        /// </remarks>
        public sealed class SecretJsonConverter : Newtonsoft.Json.JsonConverter
        {
            public override void WriteJson(
                Newtonsoft.Json.JsonWriter writer, 
                object value, 
                Newtonsoft.Json.JsonSerializer serializer) 
            =>
                writer.WriteValue(value.ToString());

            public override object ReadJson(
                Newtonsoft.Json.JsonReader reader, 
                Type objectType, 
                object existingValue,
                Newtonsoft.Json.JsonSerializer serializer) 
            =>
                throw new NotSupportedException("Cannot deserialize a Secret from JSON.");

            public override bool CanConvert(Type objectType) =>
                objectType == typeof(SecretString);
        }

        /// <summary>
        ///     Serializes a <see cref="SecretString"/> as a JSON string
        ///     using System.Text.Json
        /// </summary>
        /// <remarks>
        ///     Only supports writing (used to avoid serializing the secret itself to JSON, only serializes
        ///     the key).
        /// </remarks>
        public sealed class SecretTextJsonConverter : System.Text.Json.Serialization.JsonConverter<SecretString>
        {
            public override SecretString Read(
                ref System.Text.Json.Utf8JsonReader reader, 
                Type typeToConvert, 
                System.Text.Json.JsonSerializerOptions options) 
            =>
                throw new NotSupportedException("Cannot deserialize a Secret from JSON.");

            public override void Write(
                System.Text.Json.Utf8JsonWriter writer, 
                SecretString value,
                System.Text.Json.JsonSerializerOptions options) 
            =>
                writer.WriteStringValue(value.ToString());
        }
    }
}
