#nullable enable

namespace Altinn.App.Api.Helpers.RequestHandling
{
    /// <summary>
    /// A helper to organise the parts in a multipart
    /// </summary>
    public class RequestPart
    {
        /// <summary>
        /// The stream to access this part.
        /// </summary>
        public Stream Stream { get; set; } = default!;

        /// <summary>
        /// The file name as given in content description.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// The parts name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The content type of the part.
        /// </summary>
        public string ContentType { get; set; } = default!;

        /// <summary>
        /// The file size of the part, 0 if not given.
        /// </summary>
        public long FileSize { get; set; }
    }
}
