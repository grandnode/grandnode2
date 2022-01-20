namespace Grand.Infrastructure.Migrations
{
    public class DbVersion: IComparable<DbVersion>
    {

        public DbVersion(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        /// <summary>
        /// Gets the major version
        /// </summary>
        public readonly int Major;

        /// <summary>
        /// Gets the minor version
        /// </summary>
        public readonly int Minor;

        /// <summary>
        /// Compares the current Version object to a specified Version object and returns an indication of their relative values.
        /// </summary>
        /// <param name="otherVersion"></param>
        /// <returns></returns>
        public int CompareTo(DbVersion otherVersion)
        {
            if (Major != otherVersion.Major)
                if (Major > otherVersion.Major)
                    return 1;
                else
                    return -1;

            if (Minor != otherVersion.Minor)
                if (Minor > otherVersion.Minor)
                    return 1;
                else
                    return -1;

            return 0;

        }

        public override string ToString()
        {
            return $"{Major}.{Minor}";
        }



    }
}
