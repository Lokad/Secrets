using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace Lokad.Secrets
{
    /// <summary>
    ///     Used by <see cref="LokadSecrets.Resolve(IConfiguration)"/>
    /// </summary>
    internal sealed class OverriddenConfiguration : IConfiguration
    {
        /// <summary>
        ///     The configuration object around which the resolution wrapper
        ///     operates. Every access to a value inside this object is wrapped
        ///     in a call to <see cref="LokadSecrets.Resolve(string)"/>
        /// </summary>
        private readonly IConfiguration _wrapped;

        public OverriddenConfiguration(IConfiguration wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        /// <inheritdoc/>
        public string this[string key] 
        {
            get => _wrapped[key] is string value ? LokadSecrets.Resolve(value).Value : null;
            set => throw new NotSupportedException(); 
        }

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            foreach (var child in _wrapped.GetChildren())
                yield return new OverriddenConfigurationSection(child);
        }

        /// <inheritdoc/>
        public IChangeToken GetReloadToken() =>
            _wrapped.GetReloadToken();

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key) =>
            new OverriddenConfigurationSection(_wrapped.GetSection(key));
    }

    /// <summary>
    ///     Used by <see cref="LokadSecrets.Resolve(IConfigurationSection)"/>
    /// </summary>
    internal sealed class OverriddenConfigurationSection : IConfigurationSection
    {
        /// <summary>
        ///     The configuration section around which the resolution wrapper
        ///     operates. Every access to a value inside this object is wrapped
        ///     in a call to <see cref="LokadSecrets.Resolve(string)"/>
        /// </summary>
        private readonly IConfigurationSection _wrapped;

        public OverriddenConfigurationSection(IConfigurationSection wrapped)
        {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        /// <inheritdoc/>
        public string this[string key]
        {
            get => _wrapped[key] is string value ? LokadSecrets.Resolve(value).Value : null;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public string Key => _wrapped.Key;

        /// <inheritdoc/>
        public string Path => _wrapped.Path;

        /// <inheritdoc/>
        public string Value
        {
            get => _wrapped.Value is string value ? LokadSecrets.Resolve(value).Value : null;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            foreach (var child in _wrapped.GetChildren())
                yield return new OverriddenConfigurationSection(child);
        }

        /// <inheritdoc/>
        public IChangeToken GetReloadToken() => _wrapped.GetReloadToken();

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key) =>
            new OverriddenConfigurationSection(_wrapped.GetSection(key));
    }
}
