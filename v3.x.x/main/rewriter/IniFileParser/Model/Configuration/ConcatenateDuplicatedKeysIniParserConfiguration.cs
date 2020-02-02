namespace Azurlane.IniFileParser.Model.Configuration
{
    public class ConcatenateDuplicatedKeysIniParserConfiguration : IniParserConfiguration
    {
        public ConcatenateDuplicatedKeysIniParserConfiguration()
        {
            ConcatenateSeparator = ";";
        }

        public ConcatenateDuplicatedKeysIniParserConfiguration(ConcatenateDuplicatedKeysIniParserConfiguration ori)
            : base(ori)
        {
            ConcatenateSeparator = ori.ConcatenateSeparator;
        }

        public new bool AllowDuplicateKeys => true;

        /// <summary>
        ///     Gets or sets the string used to concatenate duplicated keys.
        /// </summary>
        /// Defaults to ';'.
        /// </value>
        public string ConcatenateSeparator { get; set; }
    }
}