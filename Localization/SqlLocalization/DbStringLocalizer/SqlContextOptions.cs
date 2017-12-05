namespace Localization.SqlLocalizer.DbStringLocalizer
{
    public class SqlContextOptions
    {
        /// <summary>
        /// SQL Server schema on which the tables are supposed to be created, if none, database default will be used
        /// </summary>
        public string SqlSchemaName { get; set; }
        /// <summary>
        /// Connection String for Localization Model Context Database
        /// </summary>
        public string ConLocalization { get; set; }
        /// <summary>
        /// Replace end of sentence (full-stop) in English with local Symbol of end of Sentence
        /// </summary>
        public string FullStop { get; set; }
    }
}
