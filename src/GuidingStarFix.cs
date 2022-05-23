using System;
using BlueprintCore.Blueprints.Configurators;
using BlueprintCore.Actions.Builder;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Blueprints.Configurators.Classes;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.Configurators.Classes.Selection;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace GuidingStarFix
{
    public class GuidingStarFix
    {
        private static readonly string RingOfGuidingStarFeatureGuid = "93b80ce01d90e8c48ade6c3ae8f22975";
        private static readonly string RingOfGuidingStarItemGuid = "b1d2843bfc4edd44cb26e4c45375a406";
        private static readonly string RingOfGuidingStarEnemyBuffGuid = "053766b486dd6004398fdedf8d48d92c";
        private static readonly Guid my_AddInitiatorAttackRollTriggerGuid = 
            new Guid("98c590710fb44895885d8682c6964c89");
        private static readonly string GuidingStarEnemyBuffDescKey = "GuidingStarEnemyBuffDescKey";
        private static readonly string GuidingStarEnemyBuffDesc = 
            "+3 bonus on damage rolls for all companions";
        private static readonly string GuidingStarEnemyBuffDispNameKey = "GuidingStarEnemyBuffDispNameKey";
        private static readonly string GuidingStarEnemyBuffDispName = "Guiding Star Damage Bonus";
        private static readonly string GuidingStarPartyBuffGuid = "cb981ea7519b7854980408f9834b327d";

        public static void Configure()
        {
            if(!Main.Enabled)
            {
                return;
            }

            var ring = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipmentRing>(RingOfGuidingStarItemGuid);
            FeatureConfigurator GuidingStarFeatureCfg;
            // Provide a description for and unhide the enemy buff
            // Otherwise no clue whether bonus is applied or where it came from
            // Set the icon the same as the ring
            BuffConfigurator.For(RingOfGuidingStarEnemyBuffGuid).
                RemoveFromFlags(BlueprintBuff.Flags.HiddenInUi).
                SetDescription(LocalizationTool.CreateString(
                    GuidingStarEnemyBuffDescKey, GuidingStarEnemyBuffDesc)).
                SetDisplayName(LocalizationTool.CreateString(
                    GuidingStarEnemyBuffDispNameKey, GuidingStarEnemyBuffDispName)).
                SetIcon(ring.Icon).
                Configure();
            // Set the damage bonus to +3 -- original blueprint has +4 for some reason
            BuffConfigurator.For(GuidingStarPartyBuffGuid).EditComponent<DamageBonusAgainstFactOwner>(
                c => c.DamageBonus = 3).Configure();

            // The implementation of this feature, per original, is to apply a (de)buff to appropriate targets
            BlueprintBuff enemyBuff = 
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(RingOfGuidingStarEnemyBuffGuid);

            // Just re-creating the condition and acton from original blueprint here, exactly
            // The debuff will be a applied with a different trigger
            ContextConditionCompareTargetHP hpCond = ElementTool.Create<ContextConditionCompareTargetHP>();
            //ContextConditionCompareTargetHP hpCond = new ContextConditionCompareTargetHP();
            hpCond.Value = ContextValues.Property(UnitProperty.MaxHP);
            hpCond.m_CompareType = ContextConditionCompareTargetHP.CompareType.Less;
            hpCond.Not = true;
            var actionApplyBuff = ElementTool.Create<ContextActionApplyBuff>();
            actionApplyBuff.m_Buff = enemyBuff.ToReference<BlueprintBuffReference>();
            actionApplyBuff.DurationValue = ContextDuration.Fixed(1);
            var conditions = ConditionsBuilder.New().Add(hpCond);
            ActionsBuilder newActions = ActionsBuilder.New().Conditional(conditions,
                ifTrue:
                    ActionsBuilder.New().Add(actionApplyBuff)
            );

            // The crux of the change/fix.  The original Trigger (AddInitiatorAttackWithWeaponTrigger)
            // checks the target's HP *after* a hit.
            // Thus, the target was rarely undamaged at the check (only if damage reduced
            // to zero.
            // Reconfigure the feature with an AddInitiatorAttackRollTrigger (onlyHit = true)
            GuidingStarFeatureCfg = FeatureConfigurator.For(RingOfGuidingStarFeatureGuid);
            // Remove the original trigger
            GuidingStarFeatureCfg.RemoveComponents(
                c => c is Kingmaker.UnitLogic.Mechanics.Components.AddInitiatorAttackWithWeaponTrigger);
            // Add the new trigger
            GuidingStarFeatureCfg.AddInitiatorAttackRollTrigger(newActions, onlyHit: true);
            // AddInitiatorAttackRollTrigger doesn't seem to assign this a name/guid.  Using EditComponent
            // to do so.  Not sure if necessary
            GuidingStarFeatureCfg.EditComponent<AddInitiatorAttackRollTrigger>(
                c => c.name = "$" + c.GetType().Name + "$" + 
                    my_AddInitiatorAttackRollTriggerGuid.ToString()
            );
            //AddInitiatorAttackRollTrigger newComp = new AddInitiatorAttackRollTrigger();
            //newComp.name = 
            //newComp.Action = newActions.Build();
            //newComp.OnlyHit = true;
            //GuidingStarFeatureCfg.AddComponent(newComp);
            GuidingStarFeatureCfg.Configure();
        }
    }
}
