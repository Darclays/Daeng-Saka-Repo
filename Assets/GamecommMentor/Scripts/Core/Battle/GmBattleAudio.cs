using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    internal static class GmBattleAudio
    {
        internal const string Folder = GmGameplayAudio.EffectsFolder + "Battle/";

        internal static string KeyFor(GmBattleMove move, GmBattleUnit actor)
        {
            if (move != GmBattleMove.BasicAttack) return move.AudioKey;
            if (actor.IsParty)
            {
                return actor.Character switch
                {
                    GmBattleCharacter.Saka => "BasicSaka",
                    GmBattleCharacter.Pacca => "BasicPacca",
                    GmBattleCharacter.Daung => "BasicDaung",
                    _ => "BasicGeneric"
                };
            }
            if (actor.IsBoss) return "CaneStrike";
            return actor.EnemyKind switch
            {
                GmBattleEnemyKind.Poppo => "BasicPoppo",
                GmBattleEnemyKind.BabiRusa => "BasicBabiRusa",
                GmBattleEnemyKind.Manguni => "BasicManguni",
                GmBattleEnemyKind.LaWuna => "BasicLaWuna",
                _ => "BasicGeneric"
            };
        }

        internal static AudioClip MoveClip(string key, bool contact) => string.IsNullOrEmpty(key) ? null :
            GmGameplayAudio.Load(Folder + key + (contact ? "_Hit" : "_Prep"));
    }
}
