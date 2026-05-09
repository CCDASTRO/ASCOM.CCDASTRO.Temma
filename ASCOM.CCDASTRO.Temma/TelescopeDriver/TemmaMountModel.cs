namespace ASCOM.CCDASTROTemma.Telescope
{
    /// <summary>
    /// Supported Takahashi Temma mount models.
    ///
    /// The original VB driver used slightly different command behavior
    /// depending on the mount generation.
    /// </summary>
    public enum TemmaMountModel
    {
        Unknown = 0,
        TemmaPC = 1,
        Temma2 = 2,
        Temma2Jr = 3,
        Temma2Z = 4,
        Temma2M = 5
    }
}
