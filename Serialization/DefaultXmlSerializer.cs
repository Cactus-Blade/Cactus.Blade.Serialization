﻿using Cactus.Blade.Guard;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Cactus.Blade.Serialization
{
    /// <summary>
    /// An XML implementation of the <see cref="ISerializer"/> interface using <see cref="XmlSerializer"/>.
    /// </summary>
    public class DefaultXmlSerializer : ISerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultJsonSerializer"/> class.
        /// </summary>
        /// <param name="name">The name of the serializer, used to when selecting which serializer to use.</param>
        /// <param name="namespaces">The object that defines the prefixes for serialization.</param>
        /// <param name="writerSettings">The object that defines the settings for the <see cref="XmlWriter"/>.</param>
        /// <param name="readerSettings">The object that defines the settings for the <see cref="XmlReader"/>.</param>
        public DefaultXmlSerializer(string name = "default", XmlQualifiedName[] namespaces = null, XmlWriterSettings writerSettings = null, XmlReaderSettings readerSettings = null)
        {
            Name = name ?? "default";
            WriterSettings = writerSettings;
            ReaderSettings = readerSettings;

            if (namespaces.IsNotNull())
                Namespaces = new XmlSerializerNamespaces(namespaces);
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets the object that defines the prefixes for serialization.
        /// </summary>
        public XmlSerializerNamespaces Namespaces { get; }

        /// <summary>
        /// Gets the object that defines the settings for the <see cref="XmlWriter"/>.
        /// </summary>
        public XmlWriterSettings WriterSettings { get; }

        /// <summary>
        /// Gets the object that defines the settings for the <see cref="XmlReader"/>.
        /// </summary>
        public XmlReaderSettings ReaderSettings { get; }

        /// <inheritdoc />
        public void SerializeToStream(Stream stream, object item, Type type)
        {
            Guard.Guard.Against.Null(stream, nameof(stream));
            Guard.Guard.Against.Null(item, nameof(item));
            Guard.Guard.Against.Null(type, nameof(type));

            type = CheckType(type, item);

            if (WriterSettings.IsNull())
                new XmlSerializer(type).Serialize(stream, item, Namespaces);
            else
                using (var xmlWriter = XmlWriter.Create(stream, WriterSettings))
                    new XmlSerializer(type).Serialize(xmlWriter, item, Namespaces);
        }

        /// <inheritdoc />
        public object DeserializeFromStream(Stream stream, Type type)
        {
            Guard.Guard.Against.Null(stream, nameof(stream));
            Guard.Guard.Against.Null(type, nameof(type));

            if (ReaderSettings.IsNull())
                return new XmlSerializer(type).Deserialize(stream);

            using var xmlReader = XmlReader.Create(stream, ReaderSettings);

            return new XmlSerializer(type).Deserialize(xmlReader);
        }

        /// <inheritdoc />
        public string SerializeToString(object item, Type type)
        {
            Guard.Guard.Against.Null(item, nameof(item));
            Guard.Guard.Against.Null(type, nameof(type));

            type = CheckType(type, item);

            var builder = new StringBuilder();

            if (WriterSettings.IsNull())
                using (var writer = new EncodedStringWriter(builder))
                    new XmlSerializer(type).Serialize(writer, item, Namespaces);
            else
                using (var stringWriter = new EncodedStringWriter(builder, WriterSettings.Encoding))
                using (var writer = XmlWriter.Create(stringWriter, WriterSettings))
                    new XmlSerializer(type).Serialize(writer, item, Namespaces);

            return builder.ToString();
        }

        /// <inheritdoc />
        public object DeserializeFromString(string data, Type type)
        {
            Guard.Guard.Against.Null(data, nameof(data));
            Guard.Guard.Against.Null(type, nameof(type));

            if (ReaderSettings.IsNull())
                using (var reader = new StringReader(data))
                    return new XmlSerializer(type).Deserialize(reader);

            using (var reader = new StringReader(data))
            using (var xmlReader = XmlReader.Create(reader, ReaderSettings))
                return new XmlSerializer(type).Deserialize(xmlReader);
        }

        /// <summary>
        /// If <paramref name="type"/> is abstract, return item.GetType().
        /// If <paramref name="type"/> is not abstract, return it.
        /// </summary>
        /// <remarks>
        /// This check allows us to handle an abstract type during
        /// serialization. There's still nothing that can be done when
        /// deserializing.
        /// </remarks>
        private static Type CheckType(Type type, object item)
        {
            return !type.GetTypeInfo().IsAbstract ? type : item.GetType();
        }

        private class EncodedStringWriter : StringWriter
        {
            private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
            public EncodedStringWriter(StringBuilder builder, Encoding encoding = null) : base(builder) { Encoding = encoding ?? Utf8; }
            public override Encoding Encoding { get; }
        }
    }
}
