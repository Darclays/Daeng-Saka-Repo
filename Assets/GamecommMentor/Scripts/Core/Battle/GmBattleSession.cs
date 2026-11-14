using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool isBoss;

        public GmBattleStats(string name, int hp, int mp, int initialMp, int speed, int damage,
            GmBattleCharacter character = GmBattleCharacter.Unspecified, bool isBoss = false)
        {
            displayName = name;
            maxHp = hp;
            maxMp = mp;
            startingMp = initialMp;
            this.speed = speed;
            attackDamage = damage;
            this.character = character;
            this.isBoss = isBoss;
        }
    }

    internal enum GmBattlePhase { Party, Enemy, Victory, Defeat }

    internal sealed class GmBattleUnit
    {
        private readonly Dictionary<GmBattleMove, int> cooldowns = new Dictionary<GmBattleMove, int>();
        private readonly Dictionary<GmBattleMove, int> buffEndRounds = new Dictionary<GmBattleMove, int>();

        public GmBattleUnit(GmBattleStats stats, bool isParty)
        {
            IsParty = isParty;
            IsBoss = !isParty && stats.isBoss;
            Name = string.IsNullOrWhiteSpace(stats.displayName)
                ? (isParty ? "Anggota Party" : "Musuh") : stats.displayName;
            MaxHp = Math.Max(1, stats.maxHp);
            MaxMp = Math.Max(0, stats.maxMp);
            Speed = Math.Max(1, stats.speed);
            AttackDamage = Math.Max(1, stats.attackDamage);
            Hp = MaxHp;
            Mp = Math.Max(0, Math.Min(stats.startingMp, MaxMp));
            Character = isParty ? ResolveCharacter(stats) : GmBattleCharacter.Unspecified;
            Skills = GmBattleMove.ForCharacter(Character);
        }

        public bool IsParty { get; }
        public bool IsBoss { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int Speed { get; }
        public int AttackDamage { get; }
        public GmBattleCharacter Character { get; }
        public IReadOnlyList<GmBattleMove> Skills { get; }
        public bool IsAlive => Hp > 0;
        public int Hp { get; internal set; }
        public int Mp { get; internal set; }
        public int LastActedRound { get; internal set; }
        public int GuardRound { get; internal set; }
        public int CompletedActions { get; internal set; }
        public bool IsStunned { get; internal set; }

        public int CooldownRemaining(GmBattleMove move) => cooldowns.TryGetValue(move, out int readyAfter)
            ? Math.Max(0, readyAfter - CompletedActions) : 0;

        public void StartCooldown(GmBattleMove move)
        {
            if (move.Cooldown > 0) cooldowns[move] = CompletedActions + move.Cooldown + 1;
        }

        public void ApplyBuff(GmBattleMove move, int round)
        {
            // A self attack buff can first improve an action on the caster's next round.
            bool countsFromNextRound = move.Target == GmBattleTarget.Self && move.Buff == GmBattleBuff.Attack;
            buffEndRounds[move] = round + move.BuffRounds - (countsFromNextRound ? 0 : 1);
        }

        public int BuffPercent(GmBattleBuff buff, int round)
        {
            int percent = 0;
            foreach (var active in buffEndRounds)
                if (active.Key.Buff == buff && active.Value >= round)
                    percent = Math.Max(percent, active.Key.BuffPercent);
            return percent;
        }

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
        public const int GuardReductionPercent = 50;
        private readonly GmBattleUnit[] units;
        private readonly int[] enemyOrder;
        private readonly int restRecovery;
        private readonly Random random = new Random();
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
        public int RechargeAmount => IsPartyTurn ? Math.Min(restRecovery, Actor.MaxMp - Actor.Mp) : 0;

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
            if (!Actor.Skills.Contains(move)) return "Tidak tersedia";
            int cooldown = Actor.CooldownRemaining(move);
            if (cooldown > 0) return $"CD {cooldown}";
            return Actor.Mp < move.MpCost ? "MP kurang" : null;
        }

        public bool CanTarget(GmBattleMove move, int index)
        {
            if (move == null || Actor == null || index < 0 || index >= units.Length || !units[index].IsAlive) return false;
            GmBattleUnit unit = units[index];
            bool ally = unit.IsParty == Actor.IsParty;
            return move.Target switch
            {
                GmBattleTarget.Self => index == ActorIndex,
                GmBattleTarget.Ally => ally,
                GmBattleTarget.AllAllies => ally,
                _ => !ally
            };
        }

        public int[] TargetsFor(GmBattleMove move, int targetIndex)
        {
            if (move == null) return Array.Empty<int>();
            if (move.Target == GmBattleTarget.Self) targetIndex = ActorIndex;
            if (!move.TargetsAll)
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
            Actor.StartCooldown(move);
            if (move == GmBattleMove.HpItem) hpItems--;
            if (move == GmBattleMove.MpItem) mpItems--;

            hits = new GmBattleHit[targets.Length];
            for (int index = 0; index < targets.Length; index++)
            {
                int targetIndexInBattle = targets[index];
                GmBattleUnit target = units[targetIndexInBattle];
                int amount = ApplyMove(move, target);
                bool stunned = move.Effect == GmBattleEffect.StunDamage && target.IsAlive &&
                    (!target.IsBoss || random.Next(100) < move.BossStunChance);
                if (stunned) target.IsStunned = true;
                hits[index] = new GmBattleHit(targetIndexInBattle, amount, stunned);
            }
            if (move.IsDamaging && move.Buff != GmBattleBuff.None) Actor.ApplyBuff(move, RoundNumber);
            if (!move.IsItem) CompleteAction();
            return true;
        }

        private int ApplyMove(GmBattleMove move, GmBattleUnit target)
        {
            if (move.Effect == GmBattleEffect.Heal || move.Effect == GmBattleEffect.HealPercent)
            {
                int heal = move.Effect == GmBattleEffect.HealPercent ? (int)((long)target.MaxHp * move.Amount / 100) : move.Amount;
                int recovered = Math.Min(heal, target.MaxHp - target.Hp);
                target.Hp += recovered;
                return recovered;
            }
            if (move.Effect == GmBattleEffect.RestoreMp)
            {
                int recovered = Math.Min(move.Amount, target.MaxMp - target.Mp);
                target.Mp += recovered;
                return recovered;
            }
            if (move.Effect == GmBattleEffect.Buff)
            {
                target.ApplyBuff(move, RoundNumber);
                return move.BuffPercent;
            }

            long power = move == GmBattleMove.BasicAttack ? Actor.AttackDamage : move.Amount;
            power += power * Actor.BuffPercent(GmBattleBuff.Attack, RoundNumber) / 100;
            power -= power * target.BuffPercent(GmBattleBuff.Defense, RoundNumber) / 100;
            // Round the reduction down so a guarded 1-damage hit still deals 1.
            if (Phase == GmBattlePhase.Enemy && target.GuardRound == RoundNumber)
                power -= power * GuardReductionPercent / 100;
            int damage = (int)Math.Min(target.Hp, power);
            target.Hp -= damage;
            return damage;
        }

        public bool TryRest(out int recovered)
        {
            recovered = 0;
            if (!IsPartyTurn || Actor.IsStunned) return false;

            recovered = RechargeAmount;
            Actor.Mp += recovered;
            CompleteAction();
            return true;
        }

        public bool TryGuard()
        {
            if (!IsPartyTurn || Actor.IsStunned) return false;

            Actor.GuardRound = RoundNumber;
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
            Actor.CompletedActions++;
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
