using MultithreadDownload.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadDownload.Utils
{
    /// <summary>
    /// The class that is representing the formatted speed information.
    /// </summary>
    public sealed class FormattedSpeed
    {
        #region Properties
        /// <summary>
        /// The change in number of bytes downloaded per second
        /// </summary>
        public long DeltaBytesPerUnitTime { get; init; }

        /// <summary>
        /// The suitable data unit used for formatting according to the given bytes count and data unit type
        /// </summary>
        public DataUnit SuitableDataUnit { get; init; }

        /// <summary>
        /// The formatted speed string by using the suitable data unit, which is a human-readable representation of the download speed, such as "500 KB/s" or "1.5 MB/s".
        /// </summary>
        public string SuitableFormattedString { get; init; }

        /// <summary>
        /// The formatted speed option
        /// </summary>
        /// <remarks>
        /// This property holds the options used for formatting the speed, 
        /// including the data unit style, decimal places, and time unit style.
        /// </remarks>
        public SpeedFormatOption Option { get; init; }

        #endregion

        #region Non-static Methods

        public FormattedSpeed(long deltaBytesPerUnitTime, SpeedFormatOption option)
        {
            // Initialize the properties of the FormattedSpeed instance
            this.DeltaBytesPerUnitTime = deltaBytesPerUnitTime;
            this.Option = option;
            this.SuitableDataUnit = this.GetSuitableDataUnit();
            this.SuitableFormattedString = this.GetFormattedString(this.SuitableDataUnit);
        }

        /// <summary>
        /// Formats the data transfer rate as a string using the specified data and time units.
        /// </summary>
        /// <param name="dataUnit">The data unit to use for formatting the speed (e.g., Bit, Byte, KiB, MiB, etc.).</param>
        /// <remarks>
        /// This method supports both basic units (bit and byte) and larger units (such as
        /// kilobyte, megabyte, etc.) according to the selected data unit type (IEC or SI). 
        /// The result is rounded to the number of decimal places specified in the options. The formatted string includes the correct unit symbols
        /// for both data and time, making it suitable for display in user interfaces or logs.
        /// </remarks>
        /// <returns>
        /// A formatted string representing the data transfer rate, 
        /// including the appropriate unit symbol for both data and time.
        /// </returns>
        public string GetFormattedString(DataUnit dataUnit)
        {
            // Return the formatted string based on the specified speed unit

            // If the specified unit is Bit or Byte, handle them separately because they are basic unit and do not require conversion
            // If the specified unit is Bit, convert bytes to bits by multiplying by 8 and return the formatted string
            if (dataUnit == DataUnit.Bit)
            {
                return $"{DeltaBytesPerUnitTime * 8} {this.Option.DataUnitInfo.GetSymbol(dataUnit)}/{this.Option.TimeUnitInfo.GetSymbol()}";
            }
            // If the specified unit is Byte, return the formatted string by the property DeltaBytesPerSecond
            else if (dataUnit == DataUnit.Byte)
            {
                return $"{DeltaBytesPerUnitTime} {this.Option.DataUnitInfo.GetSymbol(dataUnit)}/{this.Option.TimeUnitInfo.GetSymbol()}";
            }

            // Otherwise, handle the units that require conversion
            // Get the base unit number according to the specified data unit type
            int baseUnitNumber = dataUnit.GetUnitType() == DataUnitType.IEC ? 1024 : 1000;
            // Get the initional exponent according to the specified data unit
            int exponent = dataUnit switch
            {
                DataUnit.KiB or DataUnit.kB => 1,
                DataUnit.MiB or DataUnit.MB => 2,
                DataUnit.GiB or DataUnit.GB => 3,
                DataUnit.TiB or DataUnit.TB => 4,
                _ => 0
            };
            return $"{Math.Round(DeltaBytesPerUnitTime / Math.Pow(baseUnitNumber, exponent), this.Option.DecimalPlaces)} {this.Option.DataUnitInfo.GetSymbol(dataUnit)}/{this.Option.TimeUnitInfo.GetSymbol()}";
        }

        /// <summary>
        /// Returns a new instance of the formatted speed using the specified format option and data unit type.
        /// </summary>
        /// <param name="formatOption">The format option that determines how the speed value is displayed.</param>
        /// <param name="unitType">The data unit type to use for representing the speed value.</param>
        /// <returns>A new <see cref="FormattedSpeed"/> instance that represents the speed formatted according to the specified
        /// options.</returns>
        public FormattedSpeed WithFormat(SpeedFormatOption formatOption)
        {
            // Return a new instance of FormattedSpeed with the specified format option and data unit type
            return new FormattedSpeed(this.DeltaBytesPerUnitTime, formatOption);
        }


        /// <summary>
        /// Get suitable data unit according to the given bytes count and data unit type
        /// </summary>
        /// <param name="bytesCount">The number of bytes</param>
        /// <param name="unitType">The type of data unit</param>
        /// <returns></returns>
        private DataUnit GetSuitableDataUnit()
        {
            // If the data unit type is Basic, return Byte.
            if (this.Option.DataUnitInfo.UnitType == DataUnitType.Basic)
            {
                return DataUnit.Byte ;
            }
            // Get the base unit number according to the specified data unit type
            int baseUnitNumber = this.Option.DataUnitInfo.UnitType == DataUnitType.IEC ? 1024 : 1000;
            // If the bytes count is greater than or equal to the base unit number raised to the power of 4, return TiB or TB
            if (this.DeltaBytesPerUnitTime >= Math.Pow(baseUnitNumber, 4))
            {
                return this.Option.DataUnitInfo.UnitType == DataUnitType.IEC ? DataUnit.TiB : DataUnit.TB;
            }
            // If the bytes count is greater than or equal to the base unit number raised to the power of 3, return GiB or GB
            else if (this.DeltaBytesPerUnitTime >= Math.Pow(baseUnitNumber, 3))
            {
                return this.Option.DataUnitInfo.UnitType == DataUnitType.IEC ? DataUnit.GiB : DataUnit.GB;
            }
            // If the bytes count is greater than or equal to the base unit number raised to the power of 2, return MiB or MB
            else if (this.DeltaBytesPerUnitTime >= Math.Pow(baseUnitNumber, 2))
            {
                return this.Option.DataUnitInfo.UnitType == DataUnitType.IEC ? DataUnit.MiB : DataUnit.MB;
            }
            // If the bytes count is greater than or equal to the base unit number, return KiB or kB
            else if (this.DeltaBytesPerUnitTime >= baseUnitNumber)
            {
                return this.Option.DataUnitInfo.UnitType == DataUnitType.IEC ? DataUnit.KiB : DataUnit.kB;
            }
            // If the bytes count is greater than or equal to 8, return Byte
            else if (this.DeltaBytesPerUnitTime >= 8)
            {
                return DataUnit.Byte;
            }
            // Otherwise, return Bit
            else
            {
                return DataUnit.Bit;
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Return a instance of FormattedSpeed from the given delta bytes and delta time
        /// </summary>
        /// <param name="deltaBytes">The change in bytes during the given time</param>
        /// <param name="deltaTime">The change in time during the change in bytes</param>
        /// <returns>A instance of <see cref="FormattedSpeed"/></returns>
        public static FormattedSpeed Format(long deltaBytes, TimeSpan deltaTime, SpeedFormatOption option)
        {
            // Convert delta time from TimeSpan to the value with specified time unit
            double deltaTimeInUnit = deltaTime.TotalSeconds;
            switch (option.TimeUnitInfo.Unit)
            {
                // If the time unit is Minute, convert delta time to minutes
                case TimeUnit.Minute:
                    deltaTimeInUnit = deltaTime.TotalMinutes;
                    break;
                // If the time unit is Hour, convert delta time to hours
                case TimeUnit.Hour:
                    deltaTimeInUnit = deltaTime.TotalHours;
                    break;
                // If the time unit is Year, convert delta time to years
                case TimeUnit.Year:
                    deltaTimeInUnit = deltaTime.TotalDays / 365;
                    break;
            }
            // Calculate the change in bytes per specified time unit and convert the result to long
            long deltaBytesPreUnitTime = (long)(deltaBytes / deltaTimeInUnit);
            // Create a new instance of FormattedSpeed and set its properties
            return new FormattedSpeed(deltaBytesPreUnitTime, option);
        }

        #endregion
    }

    /// <summary>
    /// The option of speed format.
    /// </summary>
    public sealed class SpeedFormatOption
    {
        /// <summary>
        /// The configuration information for data unit formatting used by this instance.
        /// </summary>
        public DataUnitInfo DataUnitInfo { get; init; } = new DataUnitInfo(DataUnitType.IEC, UnitSymbolStyle.Symbol);

        /// <summary>
        /// The configuration information for time unit formatting used by this instance.
        /// </summary>
        public TimeUnitInfo TimeUnitInfo { get; init; } = new TimeUnitInfo(TimeUnit.Second, UnitSymbolStyle.Symbol);

        /// <summary>
        /// The number of decimal places to round the formatted speed value to. 
        /// </summary>
        /// <remarks>
        /// This determines the precision of the displayed speed, allowing for more or fewer decimal places as needed for readability and accuracy.
        /// </remarks>
        public int DecimalPlaces { get; init; } = 2;

        /// <summary>
        /// Creates a new instance of the SpeedFormatOption class with default settings.
        /// </summary>
        /// <returns>
        /// A SpeedFormatOption object initialized with default values.
        /// </returns>
        public static SpeedFormatOption CreateOption()
        {
            return new SpeedFormatOption();
        }

        /// <summary>
        /// Creates a new instance of the speed formatting option using the specified data unit, time unit, and decimal
        /// precision.
        /// </summary>
        /// <param name="dataUnitInfo">
        /// The data unit information to use for formatting speed values. Cannot be null.
        /// </param>
        /// <param name="timeUnitInfo">
        /// The time unit information to use for formatting speed values. Cannot be null.
        /// </param>
        /// <param name="decimalPlaces">
        /// The number of decimal places to display in formatted speed values. Must be zero or greater.
        /// </param>
        /// <returns>
        /// A new instance of SpeedFormatOption configured with the provided units and decimal precision.
        /// </returns>
        public static SpeedFormatOption CreateOption(DataUnitInfo dataUnitInfo, TimeUnitInfo timeUnitInfo, int decimalPlaces)
        {
            return new SpeedFormatOption(dataUnitInfo, timeUnitInfo, decimalPlaces);
        }

        private SpeedFormatOption() { }

        private SpeedFormatOption(DataUnitInfo dataUnitInfo, TimeUnitInfo timeUnitInfo, int decimalPlaces)
        {
            this.DataUnitInfo = dataUnitInfo;
            this.TimeUnitInfo = timeUnitInfo;
            this.DecimalPlaces = decimalPlaces;
        }
    }

    /// <summary>
    /// The information of displayed time unit.
    /// </summary>
    public readonly struct TimeUnitInfo
    {
        /// <summary>
        /// The displayed time unit.
        /// </summary>
        public TimeUnit Unit { get; }

        /// <summary>
        /// The displayed time unit style.
        /// </summary>
        public UnitSymbolStyle Style { get; }

        public TimeUnitInfo(TimeUnit unit, UnitSymbolStyle style)
        {
            this.Unit = unit;
            this.Style = style;
        }

        /// <summary>
        /// Get the unit symbol string.
        /// </summary>
        /// <param name="unit">The required unit</param>
        /// <returns>The required symbol of the unit</returns>
        public string GetSymbol() => (this.Unit, this.Style) switch
        {
            (TimeUnit.Second, UnitSymbolStyle.Symbol) => "s",
            (TimeUnit.Second, UnitSymbolStyle.FullName) => "second",

            (TimeUnit.Minute, UnitSymbolStyle.Symbol) => "min",
            (TimeUnit.Minute, UnitSymbolStyle.FullName) => "minute",

            (TimeUnit.Hour, UnitSymbolStyle.Symbol) => "h",
            (TimeUnit.Hour, UnitSymbolStyle.FullName) => "hour",

            (TimeUnit.Year, UnitSymbolStyle.Symbol) => "yr",
            (TimeUnit.Year, UnitSymbolStyle.FullName) => "year",
            _ => this.Unit.ToString()
        };
    }

    /// <summary>
    /// The time unit for showing speed.
    /// </summary>
    public enum TimeUnit
    {
        Second = 0,
        Minute = 1,
        Hour = 2,
        Year = 3
    }

    /// <summary>
    /// The information of displayed data unit.
    /// </summary>
    public readonly struct DataUnitInfo
    {
        /// <summary>
        /// The tpye of the data unit used for formatting
        /// </summary>
        public DataUnitType UnitType { get; }

        /// <summary>
        /// The displayed unit style.
        /// </summary>
        public UnitSymbolStyle Style { get; }

        public DataUnitInfo(DataUnitType unitType, UnitSymbolStyle style)
        {
            this.UnitType = unitType;
            this.Style = style;
        }

        /// <summary>
        /// Get the unit symbol string.
        /// </summary>
        /// <param name="unit">The required unit</param>
        /// <returns>The required symbol of the unit</returns>
        public string GetSymbol(DataUnit unit) => (unit, this.Style) switch
        {
            (DataUnit.Byte, UnitSymbolStyle.Symbol) => "B",
            (DataUnit.Byte, UnitSymbolStyle.FullName) => "Byte",

            (DataUnit.KiB, UnitSymbolStyle.Symbol) => "KiB",
            (DataUnit.KiB, UnitSymbolStyle.FullName) => "Kibibyte",

            (DataUnit.MiB, UnitSymbolStyle.Symbol) => "MiB",
            (DataUnit.MiB, UnitSymbolStyle.FullName) => "Mibibyte",

            (DataUnit.GiB, UnitSymbolStyle.Symbol) => "GiB",
            (DataUnit.GiB, UnitSymbolStyle.FullName) => "Gibibyte",

            (DataUnit.TiB, UnitSymbolStyle.Symbol) => "TiB",
            (DataUnit.TiB, UnitSymbolStyle.FullName) => "Tebibyte",

            (DataUnit.kB, UnitSymbolStyle.Symbol) => "kB",
            (DataUnit.kB, UnitSymbolStyle.FullName) => "kilobyte",

            (DataUnit.MB, UnitSymbolStyle.Symbol) => "MB",
            (DataUnit.MB, UnitSymbolStyle.FullName) => "Mibibyte",

            (DataUnit.GB, UnitSymbolStyle.Symbol) => "GB",
            (DataUnit.GB, UnitSymbolStyle.FullName) => "Gibibyte",

            (DataUnit.TB, UnitSymbolStyle.Symbol) => "TB",
            (DataUnit.TB, UnitSymbolStyle.FullName) => "Tebibyte",
            _ => unit.ToString()
        };
    }

    /// <summary>
    /// The type of data unit.
    /// </summary>
    public enum DataUnitType
    {
        Basic = 0,
        IEC = 1,
        Metric = 2
    }

    /// <summary>
    /// The data unit.
    /// </summary>
    public enum DataUnit
    {
        Bit = 0,
        Byte = 1,

        KiB = 2,
        MiB = 3,
        GiB = 4,
        TiB = 5,

        kB = 6,
        MB = 7,
        GB = 8,
        TB = 9,
    }

    /// <summary>
    /// The type of data unit style.
    /// </summary>
    public enum UnitSymbolStyle
    {
        Symbol,
        FullName
    }
}
