using System;
using System.Collections.Generic;
using System.Linq;

namespace GamecommMentor.DaengSaka
{
    internal enum GmBattleEnemyKind { Generic, Poppo, BabiRusa, Manguni, LaWuna }

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
        public GmBattleEnemyKind enemyKind;

        public static GmBattleStats[] CreateDefaultParty() => new[]
        {
            new GmBattleStats("Saka Barani", 110, 20, 8, 24, 24, GmBattleCharacter.Saka),
            new GmBattleStats("Pacca Andi", 138, 16, 5, 17, 17, GmBattleCharacter.Pacca),
            new GmBattleStats("Daung", 84, 28, 12, 20, 12, GmBattleCharacter.Daung)
        };

        public GmBattleCharacter ResolvedCharacter
        {
            get
            {
                if (character != GmBattleCharacter.Unspecified) return character;
                // Scenes saved by 1B.1A have no character field yet.
                return displayName?.Trim().ToLowerInvariant() switch
                {
                    "saka" or "saka barani" => GmBattleCharacter.Saka,
                    "pacca" or "pacca andi" => GmBattleCharacter.Pacca,
                    "daung" => GmBattleCharacter.Daung,
                    _ => GmBattleCharacter.Unspecified
                };
            }
        }

        public GmBattleStats(string name, int hp, int mp, int initialMp, int speed, int damage,
            GmBattleCharacter character = GmBattleCharacter.Unspecified, bool isBoss = false,
            GmBattleEnemyKind enemyKind = GmBattleEnemyKind.Generic)
        {
            displayName = name;
            maxHp = hp;
            maxMp = mp;
            startingMp = initialMp;
            this.speed = speed;
            attackDamage = damage;
            this.character = character;
            this.isBoss = isBoss;
            this.enemyKind = enemyKind;
        }
    }

    internal enum GmBattlePhase { Party, Enemy, Victory, Defeat, BetweenWaves }

    internal sealed class GmBattleUnit
    {
        private readonly Dictionary<GmBattleMove, int> cooldowns = new Dictionary<GmBattleMove, int>();
        private readonly Dictionary<GmBattleMove, int> buffEndRounds = new Dictionary<GmBattleMove, int>();

        private readonly GmPartyVitals vitals;

        private readonly int baseAttack;
        private readonly int weaponBonus;

        public GmBattleUnit(GmBattleStats stats, bool isParty, GmPartyVitals partyVitals = null, int weaponBonus = 0)
        {
            IsParty = isParty;
            IsBoss = !isParty && stats.isBoss;
            EnemyKind = isParty ? GmBattleEnemyKind.Generic : stats.enemyKind;
            vitals = partyVitals ?? new GmPartyVitals(stats, isParty);
            Speed = Math.Max(1, stats.speed);
            baseAttack = vitals.BaseAttack;
            this.weaponBonus = isParty ? weaponBonus : 0;
            Character = isParty ? stats.ResolvedCharacter : GmBattleCharacter.Unspecified;
            Skills = GmBattleMove.ForCharacter(Character);
        }

        public bool IsParty { get; }
        public bool IsBoss { get; }
        public GmBattleEnemyKind EnemyKind { get; }
        public GmBattleMove EnemySkill => EnemyKind switch
        {
            GmBattleEnemyKind.Poppo => GmBattleMove.GBite,
            GmBattleEnemyKind.BabiRusa => GmBattleMove.HammerSweep,
            GmBattleEnemyKind.Manguni => GmBattleMove.Uppercut,
            GmBattleEnemyKind.LaWuna => GmBattleMove.TailSweep,
            _ => null
        };
        public string Name => vitals.Name;
        public int MaxHp => vitals.MaxHp;
        public int MaxMp => vitals.MaxMp;
        public int Speed { get; }
        public int AttackDamage => PowerFor(GmBattleMove.BasicAttack);
        internal int PowerFor(GmBattleMove move) => move.PowerWithWeapon(baseAttack, weaponBonus);
        public GmBattleCharacter Character { get; }
        public IReadOnlyList<GmBattleMove> Skills { get; }
        public bool IsPresent { get; internal set; } = true;
        public bool IsAlive => IsPresent && Hp > 0;
        public int Hp { get => vitals.Hp; internal set => vitals.Hp = value; }
        public int Mp { get => vitals.Mp; internal set => vitals.Mp = value; }
        public int LastActedRound { get; internal set; } = -1;
        public int GuardRound { get; internal set; } = -1;
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
            // A self attack buff that uses the caster's action benefits their next round first.
            bool countsFromNextRound = !move.IsFreeAction && move.Target == GmBattleTarget.Self && move.Buff == GmBattleBuff.Attack;
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
    }

    internal sealed class GmBattleSession
    {
        public const int GuardReductionPercent = 50;
        private readonly GmBattleUnit[] units;
        private readonly int[] enemyOrder;
        private readonly int reinforcementStart;
        private readonly int restRecovery;
        private readonly Random random = new Random();
        private readonly GmPartyInventory inventory;

        public GmBattleSession(GmBattleStats[] party, GmBattleStats[] enemies, int restMp,
            GmBossBattleProfile bossProfile = null, GmPartyRun run = null, GmBattleStats[] reinforcements = null)
        {
            if (bossProfile != null) enemies = bossProfile.CreateRoster();
            if (party == null || party.Length == 0 || enemies == null || enemies.Length == 0)
                throw new ArgumentException("Battle needs at least one party member and one enemy.");

            run ??= GmPartyRun.Current;
            inventory = run.Inventory;
            GmPartyVitals[] members = run.BindRoster(party);
            PartyCount = party.Length;
            int reserveCount = bossProfile == null ? reinforcements?.Length ?? 0 : 0;
            reinforcementStart = PartyCount + enemies.Length;
            units = new GmBattleUnit[reinforcementStart + reserveCount];
            enemyOrder = new int[enemies.Length + reserveCount];
            GmPartyEquipment equipment = run.Equipment;
            for (int i = 0; i < party.Length; i++)
            {
                int bonus = equipment.Equipped(members[i])?.Bonus ?? 0;
                units[i] = new GmBattleUnit(party[i], true, members[i], bonus);
            }
            for (int i = 0; i < enemyOrder.Length; i++)
            {
                int index = PartyCount + i;
                bool firstWave = i < enemies.Length;
                GmBattleStats stats = firstWave ? enemies[i] : reinforcements[i - enemies.Length];
                units[index] = new GmBattleUnit(stats, false) { IsPresent = firstWave };
                enemyOrder[i] = index;
            }
            Array.Sort(enemyOrder, (left, right) =>
            {
                int bySpeed = units[right].Speed.CompareTo(units[left].Speed);
                return bySpeed != 0 ? bySpeed : left.CompareTo(right);
            });
            restRecovery = Math.Max(1, restMp);
            if (bossProfile != null) Boss = new GmBossBattle(bossProfile, enemies, units, PartyCount);
            if (run.PartyWiped) Phase = GmBattlePhase.Defeat;
            else if (Boss != null)
            {
                RoundNumber = 0;
                Phase = GmBattlePhase.Enemy;
                ActorIndex = Boss.Index;
            }
        }

        public IReadOnlyList<GmBattleUnit> Units => units;
        public IReadOnlyList<int> EnemyOrder => enemyOrder;
        public int PartyCount { get; }
        public int WaveNumber { get; private set; } = 1;
        public int WaveCount => reinforcementStart < units.Length ? 2 : 1;
        public GmBossBattle Boss { get; }
        public bool IsOpeningEnemyPhase => RoundNumber == 0 && Phase == GmBattlePhase.Enemy;
        public GmBattlePhase Phase { get; private set; } = GmBattlePhase.Party;
        public int ActorIndex { get; private set; } = -1;
        public GmBattleUnit Actor => ActorIndex >= 0 ? units[ActorIndex] : null;
        public int RoundNumber { get; private set; } = 1;
        public bool IsActive => Phase == GmBattlePhase.Party || Phase == GmBattlePhase.Enemy;
        public bool IsChoosingPartyMember => Phase == GmBattlePhase.Party && ActorIndex < 0;
        public bool IsPartyTurn => Phase == GmBattlePhase.Party && ActorIndex >= 0;
        public int RechargeAmount => IsPartyTurn ? Math.Min(restRecovery, Actor.MaxMp - Actor.Mp) : 0;

        internal bool TryStartNextWave()
        {
            if (Phase != GmBattlePhase.BetweenWaves) return false;
            for (int index = PartyCount; index < units.Length; index++)
                units[index].IsPresent = index >= reinforcementStart;
            WaveNumber++;
            RoundNumber++;
            Phase = GmBattlePhase.Party;
            return true;
        }

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

        public int ItemCount(GmBattleMove move) => inventory.Count(move?.Item);

        public string UnavailableReason(GmBattleMove move)
        {
            if (Actor == null || Actor.IsStunned || move == null) return "Tidak tersedia";
            if (move == GmBattleMove.BasicAttack) return null;
            if (move.IsEnemyMove)
            {
                if (move != Actor.EnemySkill && (Boss?.Index != ActorIndex || !Boss.SupportsMove(move))) return "Tidak tersedia";
            }
            else
            {
                if (!Actor.IsParty) return "Tidak tersedia";
                if (move.IsItem)
                {
                    if (ItemCount(move) == 0) return "Habis";
                    for (int i = 0; i < PartyCount; i++)
                        if (CanTarget(move, i)) return null;
                    return move.Effect == GmBattleEffect.RestoreMp ? "MP target penuh" : "HP target penuh";
                }
                if (!Actor.Skills.Contains(move)) return "Tidak tersedia";
            }
            int cooldown = Actor.CooldownRemaining(move);
            if (cooldown > 0) return $"CD {cooldown}";
            return Actor.Mp < move.MpCost ? "MP kurang" : null;
        }

        public bool CanTarget(GmBattleMove move, int index)
        {
            if (move == null || Actor == null || index < 0 || index >= units.Length || !units[index].IsAlive) return false;
            if (move.IsEnemyMove && move.Target == GmBattleTarget.AllAllies && index == ActorIndex) return false;
            GmBattleUnit unit = units[index];
            if (move.IsItem && ((move.Effect == GmBattleEffect.Heal && unit.Hp >= unit.MaxHp) ||
                                (move.Effect == GmBattleEffect.RestoreMp && unit.Mp >= unit.MaxMp))) return false;
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

            if (move.IsItem && !inventory.TryRemove(move.Item)) return false;
            Actor.Mp -= move.MpCost;
            Actor.StartCooldown(move);

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
            if (move.IsEnemyMove && Boss?.Index == ActorIndex) Boss.ResolveMove(move);
            if (!move.IsItem && !move.IsFreeAction) CompleteAction();
            return true;
        }

        private int ApplyMove(GmBattleMove move, GmBattleUnit target)
        {
            if (move.Effect == GmBattleEffect.Summon || move.Effect == GmBattleEffect.Charge) return 0;
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

            long power = Actor.PowerFor(move);
            power += power * Actor.BuffPercent(GmBattleBuff.Attack, RoundNumber) / 100;
            power -= power * Actor.BuffPercent(GmBattleBuff.Weaken, RoundNumber) / 100;
            power -= power * target.BuffPercent(GmBattleBuff.Defense, RoundNumber) / 100;
            if (move == GmBattleMove.BasicAttack && target.IsBoss && Boss != null)
                power -= power * Boss.BasicAttackReduction / 100;
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
            if (Boss?.Index == ActorIndex) Boss.CancelCharge();
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

        public void PrepareEnemyTurn()
        {
            if (Phase == GmBattlePhase.Enemy && Boss?.Index == ActorIndex) Boss.UpdatePhase();
        }

        public GmBattleMove ChooseEnemyMove(out GmBattleMove support, out int target)
        {
            support = null;
            if (Boss?.Index == ActorIndex)
            {
                GmBattleMove move = Boss.ChooseMove(random, out support);
                target = -1;
                if (move.Target == GmBattleTarget.Enemy)
                {
                    var living = new List<int>(PartyCount);
                    for (int index = 0; index < PartyCount; index++)
                        if (units[index].IsAlive) living.Add(index);
                    if (living.Count > 0) target = living[random.Next(living.Count)];
                }
                return move;
            }
            target = FindEnemyTarget();
            GmBattleMove skill = Actor.EnemySkill;
            // Babi Rusa shows its area threat on its first turn. The other skills come in after an action.
            if (skill != null && (Actor.CompletedActions > 0 || skill == GmBattleMove.HammerSweep) &&
                UnavailableReason(skill) == null) return skill;
            return GmBattleMove.BasicAttack;
        }

        private void CompleteAction()
        {
            Actor.LastActedRound = RoundNumber;
            Actor.CompletedActions++;
            ActorIndex = -1;
            if (Boss != null && !units[Boss.Index].IsAlive) Boss.DespawnMinions();
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
                Phase = !partyAlive ? GmBattlePhase.Defeat
                    : WaveNumber < WaveCount ? GmBattlePhase.BetweenWaves : GmBattlePhase.Victory;
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
