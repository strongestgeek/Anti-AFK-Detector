using System.Diagnostics;

namespace AFKSentinel.Service
{
    public static class EventLogSource
    {
        private const string SOURCE_NAME = "AFK-Sentinel Service";
        private const string LOG_NAME = "Application"; // Standard Application Log
        public const int EVENT_ID_DETECTION = 9001; // Custom Event ID for detections

        static EventLogSource()
        {
            if (!EventLog.SourceExists(SOURCE_NAME))
            {
                // Create the event source. Note: This operation requires administrative privileges.
                // In a production environment, this should ideally be handled during installation.
                // For development, ensure the service runs with sufficient permissions or create manually.
                EventLog.CreateEventSource(SOURCE_NAME, LOG_NAME);
            }
        }

        public static void WriteEntry(string message, EventLogEntryType type, int eventId)
        {
            EventLog.WriteEntry(SOURCE_NAME, message, type, eventId);
        }
    }
}
