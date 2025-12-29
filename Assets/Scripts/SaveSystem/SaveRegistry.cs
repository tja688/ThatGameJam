using System.Collections.Generic;

namespace ThatGameJam.SaveSystem
{
    public static class SaveRegistry
    {
        private static readonly HashSet<ISaveParticipant> Participants = new HashSet<ISaveParticipant>();

        public static void Register(ISaveParticipant participant)
        {
            if (participant == null)
            {
                return;
            }

            Participants.Add(participant);
        }

        public static void Unregister(ISaveParticipant participant)
        {
            if (participant == null)
            {
                return;
            }

            Participants.Remove(participant);
        }

        public static List<ISaveParticipant> GetParticipants()
        {
            var list = new List<ISaveParticipant>(Participants.Count);
            foreach (var participant in Participants)
            {
                if (participant != null)
                {
                    list.Add(participant);
                }
            }

            return list;
        }
    }
}
