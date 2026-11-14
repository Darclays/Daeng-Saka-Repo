using System;
using System.Collections.Generic;

namespace GamecommMentor.DaengSaka
{
    [Serializable]
    internal struct GmBattleStats
    {
        public string displayName;
        public int maxHp;
        public int maxMp;
        public int startingMp;
        public int speed;
        public int attackDamage;
        public GmBattleCharacter character;

        public GmBattleStats(string name, int hp, int mp, int initialMp, int speed, int damage,
            GmBattleCharacter character = GmBattleCharacter.Unspecified)
        {
            displayName = name;
            maxHp = hp;
            maxMp = mp;
            startingMp = initialMp;
            this.speed = speed;
            attackDamage = damage;
            this.character = character;
        }
    }

    internal enum GmBattlePhase { Party, Enemy, Victory, Defeat }

    internal sealed class GmBattleUnit
    {
        public GmBattleUnit(GmBattleStats stats, bool isParty)
        {
            IsParty = isParty;
            Name = string.IsNullOrWhiteSpace(stats.displayName)
                ? (isParty ? "Anggota Party" : "Musuh") : stats.displayName;
            MaxHp = Math.Max(1, stats.maxHp);
            MaxMp = Math.Max(0, stats.maxMp);
            Speed = Math.Max(1, stats.speed);
            AttackDamage = Math.Max(1, stats.attackDamage);
            Hp = MaxHp;
            Mp = Math.Max(0, Math.Min(stats.startingMp, MaxMp));
            Character = isParty ? ResolveCharacter(stats) : GmBattleCharacter.Unspecified;
            Skill = GmBattleMove.ForCharacter(Character);
        }

        public bool IsParty { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int Speed { get; }
        public int AttackDamage { get; }
        public GmBattleCharacter Character { get; }
        public GmBattleMove Skill { get; }
        public bool IsAlive => Hp > 0;
        public int Hp { get; internal set; }
        public int Mp { get; internal set; }
        public int LastActedRound { get; internal set; }
        public int SkillCooldown { get; internal set; }
        public bool IsStunned { get; internal set; }

        private static GmBattleCharacter ResolveCharacter(GmBattleStats stats)
        {
            if (stats.character != GmBattleCharacter.Unspecified) return stats.character;
            // Scenes saved by 1B.1A have no character field yet.
            return stats.displayName?.Trim().ToLowerInvariant() switch
            {
                "saka" => GmBattleCharacter.Saka,
                "saka barani" => GmBattleCharacter.Saka,
                "pacca" => GmBattleCharacter.Pacca,
                "pacca andi" => GmBattleCharacter.Pacca,
                "daung" => GmBattleCharacter.Daung,
                _ => GmBattleCharacter.Unspecified
            };
        }
    }

    internal sealed class GmBattleSession
    {
        private readonly GmBattleUnit[] units;
        private readonly int[] enemyOrder;
        private readonly int restRecovery;
        private int hpItems = 1;
        private int mpItems = 1;

        public GmBattleSession(GmBattleStats[] party, GmBattleStats[] enemies, int restMp)
        {
            if (party == null || party.Length == 0 || enemies == null || enemies.Length == 0)
                throw new ArgumentException("Battle needs at least one party member and one enemy.");

            PartyCount = party.Length;
            units = new GmBattleUnit[party.Length + enemies.Length];
            enemyOrder = new int[enemies.Length];
            for (int i = 0; i < party.Length; i++) units[i] = new GmBattleUnit(party[i], true);
            for (int i = 0; i < enemies.Length; i++)
            {
                int index = PartyCount + i;
                units[index] = new GmBattleUnit(enemies[i], false);
                enemyOrder[i] = index;
            }
            Array.Sort(enemyOrder, (left, right) =>
            {
                int bySpeed = units[right].Speed.CompareTo(units[left].Speed);
                return bySpeed != 0 ? bySpeed : left.CompareTo(right);
            });
            restRecovery = Math.Max(1, restMp);
        }

        public IReadOnlyList<GmBattleUnit> Units => units;
        public IReadOnlyList<int> EnemyOrder => enemyOrder;
        public int PartyCount { get; }
        public GmBattlePhase Phase { get; private set; } = GmBattlePhase.Party;
        public int ActorIndex { get; private set; } = -1;
        public GmBattleUnit Actor => ActorIndex >= 0 ? units[ActorIndex] : null;
        public int RoundNumber { get; private set; } = 1;
        public bool IsActive => Phase == GmBattlePhase.Party || Phase == GmBattlePhase.Enemy;
        public bool IsChoosingPartyMember => Phase == GmBattlePhase.Party && ActorIndex < 0;
        public bool IsPartyTurn => Phase == GmBattlePhase.Party && ActorIndex >= 0;

        public int PartyActionsRemaining
        {
            get
            {
                int remaining = 0;
                for (int index = 0; index < PartyCount; index++)
                    if (CanActThisRound(units[index])) remaining++;
                return remaining;
            }
        }

        public bool CanChoosePartyMember(int index) =>
            IsChoosingPartyMember && index >= 0 && index < PartyCount && CanActThisRound(units[index]);

        public bool TryChoosePartyMember(int index)
        {
            if (!CanChoosePartyMember(index)) return false;
            ActorIndex = index;
            return true;
        }

        public bool TryDeselectPartyMember()
        {
            if (!IsPartyTurn || Actor.IsStunned) return false;
            ActorIndex = -1;
            return true;
        }

        public bool TryAttack(int targetIndex, out int damage)
        {
            damage = 0;
            if (!TryUse(GmBattleMove.BasicAttack, targetIndex, out GmBattleHit[] hits)) return false;
            damage = hits[0].Amount;
            return true;
        }

        public int ItemCount(GmBattleMove move) => move == GmBattleMove.HpItem ? hpItems : mpItems;

        public string UnavailableReason(GmBattleMove move)
        {
            if (Actor == null || Actor.IsStunned || move == null) return "Tidak tersedia";
            if (move == GmBattleMove.BasicAttack) return null;
            if (!Actor.IsParty) return "Tidak tersedia";
            if (move.IsItem) return ItemCount(move) > 0 ? null : "Habis";
            if (move != Actor.Skill) return "Tidak tersedia";
            if (Actor.SkillCooldown > 0) return $"CD {Actor.SkillCooldown}";
            return Actor.Mp < move.MpCost ? "MP kurang" : null;
        }

        public bool CanTarget(GmBattleMove move, int index)
        {
            if (move == null || Actor == null || index < 0 || index >= units.Length) return false;
            GmBattleUnit unit = units[index];
            bool ally = unit.IsParty == Actor.IsParty;
            return unit.IsAlive && ally == (move.Target == GmBattleTarget.Ally);
        }

        public int[] TargetsFor(GmBattleMove move, int targetIndex)
        {
            if (move == null) return Array.Empty<int>();
            if (move.Target != GmBattleTarget.AllEnemies)
                return CanTarget(move, targetIndex) ? new[] { targetIndex } : Array.Empty<int>();

            var targets = new List<int>(units.Length);
            for (int index = 0; index < units.Length; index++)
                if (CanTarget(move, index)) targets.Add(index);
            return targets.ToArray();
        }

        public bool TryUse(GmBattleMove move, int targetIndex, out GmBattleHit[] hits)
        {
            hits = Array.Empty<GmBattleHit>();
            if (UnavailableReason(move) != null) return false;
            int[] targets = TargetsFor(move, targetIndex);
            if (targets.Length == 0) return false;

            Actor.Mp -= move.MpCost;
            if (move.Cooldown > 0) Actor.SkillCooldown = move.Cooldown + 1;
            if (move == GmBattleMove.HpItem) hpItems--;
            if (move == GmBattleMove.MpItem) mpItems--;

            hits = new GmBattleHit[targets.Length];
            for (int index = 0; index < targets.Length; index++)
            {
                int target = targets[index];
                hits[index] = new GmBattleHit(target, ApplyMove(move, units[target]));
            }
            if (!move.IsItem) CompleteAction();
            return true;
        }

        private int ApplyMove(GmBattleMove move, GmBattleUnit target)
        {
            if (move.Effect == GmBattleEffect.Heal)
            {
                int recovered = Math.Min(move.Amount, target.MaxHp - target.Hp);
                target.Hp += recovered;
                return recovered;
            }
            if (move.Effect == GmBattleEffect.RestoreMp)
            {
                int recovered = Math.Min(move.Amount, target.MaxMp - target.Mp);
                target.Mp += recovered;
                return recovered;
            }

            int power = move == GmBattleMove.BasicAttack ? Actor.AttackDamage : move.Amount;
            int damage = Math.Min(target.Hp, power);
            target.Hp -= damage;
            if (move.Effect == GmBattleEffect.StunDamage && target.IsAlive) target.IsStunned = true;
            return damage;
        }

        public bool TryRest(out int recovered)
        {
            recovered = 0;
            if (!IsPartyTurn || Actor.IsStunned) return false;

            recovered = Math.Min(restRecovery, Actor.MaxMp - Actor.Mp);
            Actor.Mp += recovered;
            CompleteAction();
            return true;
        }

        public bool TrySkipStunnedTurn()
        {
            if (Actor == null || !Actor.IsStunned) return false;
            Actor.IsStunned = false;
            CompleteAction();
            return true;
        }

        public int FindEnemyTarget()
        {
            int target = -1;
            for (int i = 0; i < PartyCount; i++)
            {
                if (!units[i].IsAlive) continue;
                if (target < 0 || units[i].Hp < units[target].Hp) target = i;
            }
            return target;
        }

        private void CompleteAction()
        {
            Actor.LastActedRound = RoundNumber;
            if (Actor.SkillCooldown > 0) Actor.SkillCooldown--;
            ActorIndex = -1;
            bool partyAlive = false;
            bool enemiesAlive = false;
            foreach (GmBattleUnit unit in units)
            {
                if (!unit.IsAlive) continue;
                if (unit.IsParty) partyAlive = true;
                else enemiesAlive = true;
            }

            if (!partyAlive || !enemiesAlive)
            {
                Phase = partyAlive ? GmBattlePhase.Victory : GmBattlePhase.Defeat;
                return;
            }

            if (Phase == GmBattlePhase.Party && PartyActionsRemaining > 0) return;

            foreach (int index in enemyOrder)
            {
                if (!CanActThisRound(units[index])) continue;
                Phase = GmBattlePhase.Enemy;
                ActorIndex = index;
                return;
            }

            RoundNumber++;
            Phase = GmBattlePhase.Party;
        }

        private bool CanActThisRound(GmBattleUnit unit) => unit.IsAlive && unit.LastActedRound != RoundNumber;
    }
}
