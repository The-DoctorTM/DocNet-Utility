using UnityEngine;

namespace DocNet.Data
{
    public class LobbyInfo
    {
        public int maxPlayers;
        public int numberOfRounds;
        public bool friendsOnly;

        public override string ToString()
        {
            System.Reflection.FieldInfo[] fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            System.Text.StringBuilder output = new System.Text.StringBuilder();
            output.AppendLine($"=== {GetType().Name} ===");

            foreach (System.Reflection.FieldInfo field in fields)
            {
                output.AppendLine($"{field.Name}: {field.GetValue(this)}");
            }

            return output.ToString();
        }

    }
}