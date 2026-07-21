using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetStudio
{
    public static class GameManager
    {
        private static Dictionary<int, Game> Games = new Dictionary<int, Game>();
        static GameManager()
        {
            int index = 0;
            Games.Add(index++, new(GameType.Rust));
            Games.Add(index++, new(GameType.Normal));
        }
        public static int Count => Games.Count;
        public static Game GetGame(GameType gameType) => GetGame((int)gameType);
        public static Game GetGame(int index)
        {
            if (!Games.TryGetValue(index, out var format))
            {
                throw new ArgumentException("Invalid format !!");
            }

            return format;
        }

        public static Game GetGame(string name) => Games.FirstOrDefault(x => x.Value.Name == name).Value;
        public static Game[] GetGames() => Games.Values.ToArray();
        public static string[] GetGameNames() => Games.Values.Select(x => x.Name).ToArray();
        public static string SupportedGames() => $"Supported Games:\n{string.Join("\n", Games.Values.Select(x => x.Name))}";
    }

    public record Game
    {
        public string Name { get; set; }
        public GameType Type { get; }

        public Game(GameType type)
        {
            Name = type.ToString();
            Type = type;
        }

        public sealed override string ToString() => Name;
    }

    public enum GameType
    {
        Rust,
        Normal,
    }

    public static class GameTypes
    {
        public static bool IsRust(this GameType type) => type == GameType.Rust;
        public static bool IsNormal(this GameType type) => type == GameType.Normal;
        // Rust bundles are plain Unity files with no game-specific encryption or container format.
        public static bool IsPlain(this GameType type) => type == GameType.Rust || type == GameType.Normal;
    }
}
