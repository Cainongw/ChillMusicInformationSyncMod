namespace ChillMusicInformationSync.SMTC
{
    public static class SMTCStatus
    {
        public static string CurrentTitle { get;  set; } = string.Empty;


        public static string CurrentArtist { get; set; } = string.Empty;


        public static bool IsPlaying { get; set; } = false;

        public static float CurrentProgress { get; set; } = 0f;
    }
}

