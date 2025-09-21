﻿namespace GorillaInfoWatch
{
    public class Constants
    {
        public const string GUID = "dev.gorillainfowatch";
        public const string Name = "GorillaInfoWatch Demo";
        public const string Version = "1.0.0.0";

        public const int SectionCapacity = 14;

        public const float NetworkRaiseInterval = 0.25f;
        public const string NetworkPropertyKey = "InfoWatch";

        // TODO: make the menu tilt members configurable
        public const float MenuTiltAngle = 80; // calculated menu angle after this will start to visually tilt it
        public const float MenuTiltMinimum = 7.5f; // angle after previous
        public const float MenuTiltAmount = 0.95f; // angle factor after that

        public const string SignificancePlayerNameTag = "[PlayerName]";
    }
}
