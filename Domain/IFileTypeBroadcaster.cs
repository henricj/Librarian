using System;
using System.Runtime.Serialization;

namespace Nyerguds.Util
{
    public interface IFileTypeBroadcaster
    {
        /// <summary>Very short code name for this type.</summary>
        string ShortTypeName { get; }
        /// <summary>Brief name and description of the overall file type, for the types dropdown in the open file dialog.</summary>
        string ShortTypeDescription { get; }
        /// <summary>Possible file extensions for this file type.</summary>
        string[] FileExtensions { get; }
        /// <summary>Brief name and description of the specific type for each extension, for the types dropdown in the save file dialog.</summary>
        string[] DescriptionsForExtensions { get; }
        /// <summary>Supported types can always be loaded, but this indicates if save functionality to this type is also available.</summary>
        bool CanSave { get; }
    }

    /// <summary>File load exceptions. These are typically ignored in favour of checking the next type to try.</summary>
    [Serializable]
    public class FileTypeLoadException : Exception
    {
        /// <summary>USed to store the attempted load type in the Data dictionary to allow serialization.</summary>
        protected static readonly string DataAttemptedLoadedType = "AttemptedLoadedType";

        /// <summary>File type that was attempted to be loaded and threw this exception.</summary>
        public string AttemptedLoadedType
        {
            get => Data[DataAttemptedLoadedType] as string;
            set => Data[DataAttemptedLoadedType] = value;
        }

        public FileTypeLoadException() { }
        public FileTypeLoadException(string message) : base(message) { }
        public FileTypeLoadException(string message, Exception innerException) : base(message, innerException) { }
        public FileTypeLoadException(string message, string attemptedLoadedType)
            : base(message)
        {
            AttemptedLoadedType = attemptedLoadedType;
        }
        public FileTypeLoadException(string message, string attemptedLoadedType, Exception innerException)
            : base(message, innerException)
        {
            AttemptedLoadedType = attemptedLoadedType;
        }

        protected FileTypeLoadException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        { }
    }

    /// <summary>A specific subclass for header parse failure. Can be used for distinguishing internally between different versions of a type.</summary>
    [Serializable]
    public class HeaderParseException : FileTypeLoadException
    {
        public HeaderParseException() { }
        public HeaderParseException(string message) : base(message) { }
        public HeaderParseException(string message, Exception innerException) : base(message, innerException) { }
        public HeaderParseException(string message, string attemptedLoadedType) : base(message, attemptedLoadedType) { }
        public HeaderParseException(string message, string attemptedLoadedType, Exception innerException) : base(message, attemptedLoadedType, innerException) { }
        protected HeaderParseException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        { }
    }
}
