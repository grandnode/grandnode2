using Grand.Domain.Configuration;

namespace Grand.Domain.Media
{
    public class StorageSettings : ISettings
    {
        /// <summary>
        /// Gets a value indicating whether the images should be stored in data base.
        /// </summary>
        public bool PictureStoreInDb { get; set; } = true;
    }
}