// ~Beginning Of File

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using Partiality.Modloader;
using UnityEngine;
using System;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}

namespace SlugBaseFalk
{
    internal class SlugBaseFalk : PartialityMod
    {
        public SlugBaseFalk()
        {
            this.ModID = "SlugBase Falk";
            this.Version = "1.0";
            this.author = "Precipitator";
        }

        public override void OnEnable()
        {
            SlugBase.PlayerManager.RegisterCharacter(new FalkSlugcat());
        }
    }

    internal class FalkSlugcat : SlugBase.SlugBaseCharacter
    {
        public FalkSlugcat() : base("Falk", 0, 0, true)
        {
        }

        protected override void Enable()
        {
            hooks = new List<Hook>();

            /// Rivulet stuff
            // Gameplay
            On.WaterNut.Update += new On.WaterNut.hook_Update(HookWaterNutUpdate);
            On.Player.Jump += new On.Player.hook_Jump(HookPlayerJump);
            On.Player.LungUpdate += new On.Player.hook_LungUpdate(HookPlayerLungUpdate);
            On.Player.WallJump += new On.Player.hook_WallJump(HookPlayerWallJump);
            On.Player.MovementUpdate += new On.Player.hook_MovementUpdate(HookPlayerMovementUpdate);
            On.Player.UpdateAnimation += new On.Player.hook_UpdateAnimation(HookPlayerUpdateAnimation);

            /// Falk Stuff
            On.Player.Update += new On.Player.hook_Update(HookPlayerUpdate);

            // Apply graphics hooks only if FancySlugcats is being used.
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                if (assembly.FullName.Contains("FancySlugcats"))
                {
                    hooks.Add(new Hook(typeof(FancySlugcats.FancyPlayerGraphics).GetMethod("AddToContainer"), typeof(FalkSlugcat).GetMethod("HookPlayerGraphicsAddToContainer"), this));
                    hooks.Add(new Hook(typeof(FancySlugcats.FancyPlayerGraphics).GetMethod("ApplyPalette"), typeof(FalkSlugcat).GetMethod("HookPlayerGraphicsApplyPalette"), this));
                    hooks.Add(new Hook(typeof(FancySlugcats.FancyPlayerGraphics).GetMethod("Update"), typeof(FalkSlugcat).GetMethod("HookPlayerGraphicsUpdate"), this));
                    hooks.Add(new Hook(typeof(FancySlugcats.FancyPlayerGraphics).GetMethod("DrawSprites"), typeof(FalkSlugcat).GetMethod("HookPlayerGraphicsDrawSprites"), this));
                    //hooks.Add(new Hook(typeof(FancySlugcats.FancyPlayerGraphics).GetConstructor(new Type[] { typeof(PhysicalObject) }), typeof(FalkSlugcat).GetMethod("HookPlayerGraphicsctor"), this));
                    break;
                }
            }
        }

        protected override void Disable()
        {
            /// Rivulet stuff
            // Gameplay
            On.WaterNut.Update -= new On.WaterNut.hook_Update(HookWaterNutUpdate);
            On.Player.Jump -= new On.Player.hook_Jump(HookPlayerJump);
            On.Player.LungUpdate -= new On.Player.hook_LungUpdate(HookPlayerLungUpdate);
            On.Player.WallJump -= new On.Player.hook_WallJump(HookPlayerWallJump);
            On.Player.MovementUpdate -= new On.Player.hook_MovementUpdate(HookPlayerMovementUpdate);
            On.Player.UpdateAnimation -= new On.Player.hook_UpdateAnimation(HookPlayerUpdateAnimation);

            /// Falk Stuff
            On.Player.Update -= new On.Player.hook_Update(HookPlayerUpdate);

            // Remove manual hooks.
            foreach (Hook hook in hooks)
            {
                hook.Undo();
            }
        }

        public void HookPlayerGraphicsAddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, newContatiner);
            this.gills = new AxolotlGills(playerGraphics, 12);
            FSprite[] newSprites = new FSprite[12 + this.gills.numberOfSprites];
            Array.Copy(sLeaser.sprites, newSprites, sLeaser.sprites.Length);
            sLeaser.sprites = new FSprite[12 + this.gills.numberOfSprites + 1];
            Array.Copy(newSprites, sLeaser.sprites, newSprites.Length);
            this.gills.InitiateSprites(sLeaser, rCam);
            this.gills.AddToContainer(sLeaser, rCam, newContatiner);
            int shieldInt = sLeaser.sprites.Length - 1;
            sLeaser.sprites[shieldInt] = new FSprite("Futile_White", true);
            sLeaser.sprites[shieldInt].shader = rCam.game.rainWorld.Shaders["GhostDistortion"];
            sLeaser.sprites[shieldInt].alpha = 0.2f;
            rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[shieldInt]);
        }

        public void HookPlayerGraphicsApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(playerGraphics, sLeaser, rCam, palette);
            this.gills.ApplyPalette(sLeaser, rCam, palette);
        }

        public void HookPlayerGraphicsUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics playerGraphics)
        {
            if (playerGraphics.player.room != null)
            {
                this.gills.Update();
            }
            orig.Invoke(playerGraphics);
        }

        public void HookPlayerGraphicsDrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!rCam.room.game.DEBUGMODE)
            {
                this.gills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
            orig.Invoke(playerGraphics, sLeaser, rCam, timeStacker, camPos);
            if (falkAura != null)
            {
                falkAura.DisruptorDrawSprites(sLeaser);
            }
        }

        private void HookWaterNutUpdate(On.WaterNut.orig_Update orig, WaterNut waterNut, bool eu)
        {
            orig.Invoke(waterNut, eu);
            bool grabbedByPlayer = false;
            if (waterNut.grabbedBy.Count > 0)
            {
                for (int i = 0; i < waterNut.grabbedBy.Count; i++)
                {
                    if (waterNut.grabbedBy[i].grabber is Player)
                    {
                        grabbedByPlayer = true;
                        break;
                    }
                }
            }
            if (((Rock)waterNut).Submersion <= 0f && grabbedByPlayer)
            {
                waterNut.swellCounter--;
                if (waterNut.swellCounter < 1)
                {
                    waterNut.Swell();
                }
            }
        }

        private void HookPlayerJump(On.Player.orig_Jump orig, Player player)
        {
            if (!base.IsMe(player))
            {
                orig.Invoke(player);
                return;
            }

            player.feetStuckPos = null;
            float num = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
            if (((Creature)player).grasps[0] != null && player.HeavyCarry(((Creature)player).grasps[0].grabbed) && !(((Creature)player).grasps[0].grabbed is Cicada))
            {
                num += Mathf.Min(Mathf.Max(0f, ((Creature)player).grasps[0].grabbed.TotalMass - 0.2f) * 1.5f, 1.3f);
            }
            player.AerobicIncrease(1f);
            switch (player.bodyMode)
            {
                case Player.BodyModeIndex.CorridorClimb:
                    ((Creature)player).bodyChunks[0].vel.y = 6f * num;
                    ((Creature)player).bodyChunks[1].vel.y = 5f * num;
                    player.standing = true;
                    player.jumpBoost = 14f; // # 8f -> 14f
                    return;
                case Player.BodyModeIndex.WallClimb:
                    {
                        int direction;
                        if (player.canWallJump != 0)
                        {
                            direction = Math.Sign(player.canWallJump);
                        }
                        else if (((Creature)player).bodyChunks[0].ContactPoint.x != 0)
                        {
                            direction = -((Creature)player).bodyChunks[0].ContactPoint.x;
                        }
                        else
                        {
                            direction = -player.flipDirection;
                        }
                        player.WallJump(direction);
                        return;
                    }
            }
            Player.AnimationIndex animationIndex = player.animation;
            switch (animationIndex)
            {
                case Player.AnimationIndex.Roll:
                    {
                        ((Creature)player).bodyChunks[1].vel *= 0f;
                        ((Creature)player).bodyChunks[1].pos += new Vector2(5f * (float)player.rollDirection, 5f);
                        ((Creature)player).bodyChunks[0].pos = ((Creature)player).bodyChunks[1].pos + new Vector2(5f * (float)player.rollDirection, 5f);
                        float t = Mathf.InverseLerp(0f, 25f, (float)player.rollCounter);
                        ((Creature)player).bodyChunks[0].vel = RWCustom.Custom.DegToVec((float)player.rollDirection * Mathf.Lerp(60f, 35f, t)) * Mathf.Lerp(9.5f, 13.1f, t) * num;
                        ((Creature)player).bodyChunks[1].vel = RWCustom.Custom.DegToVec((float)player.rollDirection * Mathf.Lerp(60f, 35f, t)) * Mathf.Lerp(9.5f, 13.1f, t) * num;
                        player.animation = Player.AnimationIndex.RocketJump;
                        player.room.PlaySound(SoundID.Slugcat_Rocket_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                        player.rollDirection = 0;
                        break;
                    }
                default:
                    if (animationIndex != Player.AnimationIndex.LedgeGrab)
                    {
                        if (animationIndex != Player.AnimationIndex.ClimbOnBeam)
                        {
                            int num2 = player.input[0].x;
                            if (player.animation == Player.AnimationIndex.DownOnFours && ((Creature)player).bodyChunks[1].ContactPoint.y < 0 && player.input[0].downDiagonal == player.flipDirection)
                            {
                                player.animation = Player.AnimationIndex.BellySlide;
                                player.rollDirection = player.flipDirection;
                                player.rollCounter = 0;
                                player.standing = false;
                                player.room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                            }
                            else
                            {
                                player.animation = Player.AnimationIndex.None;
                                if (player.standing)
                                {
                                    if (player.slideCounter > 0 && player.slideCounter < 10)
                                    {
                                        ((Creature)player).bodyChunks[0].vel.y = 12f * num; // # 9f -> 12f
                                        ((Creature)player).bodyChunks[1].vel.y = 10f * num; // # 7f -> 10f
                                        BodyChunk bodyChunk = ((Creature)player).bodyChunks[0];
                                        bodyChunk.vel.x = bodyChunk.vel.x * 0.5f;
                                        BodyChunk bodyChunk2 = ((Creature)player).bodyChunks[1];
                                        bodyChunk2.vel.x = bodyChunk2.vel.x * 0.5f;
                                        BodyChunk bodyChunk3 = ((Creature)player).bodyChunks[0];
                                        bodyChunk3.vel.x = bodyChunk3.vel.x - (float)player.slideDirection * 4f * num;
                                        player.jumpBoost = 8f; // # 5f -> 8f
                                        player.animation = Player.AnimationIndex.Flip;
                                        player.room.PlaySound(SoundID.Slugcat_Flip_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                        player.slideCounter = 0;
                                    }
                                    else
                                    {
                                        ((Creature)player).bodyChunks[0].vel.y = 6f * num; // # 4f -> 6f
                                        ((Creature)player).bodyChunks[1].vel.y = 5f * num; // # 3f -> 5f
                                        player.jumpBoost = 8f;
                                        player.room.PlaySound((player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam) ? SoundID.Slugcat_Normal_Jump : SoundID.Slugcat_From_Horizontal_Pole_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                    }
                                }
                                else
                                {
                                    float num3 = 1.5f;
                                    if (player.superLaunchJump >= 20)
                                    {
                                        player.superLaunchJump = 0;
                                        num3 = 12f; // # 9f -> 12f
                                        num2 = ((((Creature)player).bodyChunks[0].pos.x <= ((Creature)player).bodyChunks[1].pos.x) ? -1 : 1);
                                        player.simulateHoldJumpButton = 6;
                                    }
                                    BodyChunk bodyChunk4 = ((Creature)player).bodyChunks[0];
                                    bodyChunk4.pos.y = bodyChunk4.pos.y + 6f;
                                    if (((Creature)player).bodyChunks[0].ContactPoint.y == -1)
                                    {
                                        BodyChunk bodyChunk5 = ((Creature)player).bodyChunks[0];
                                        bodyChunk5.vel.y = bodyChunk5.vel.y + 3f * num;
                                        if (num2 == 0)
                                        {
                                            BodyChunk bodyChunk6 = ((Creature)player).bodyChunks[0];
                                            bodyChunk6.vel.y = bodyChunk6.vel.y + 3f * num;
                                        }
                                    }
                                    BodyChunk bodyChunk7 = ((Creature)player).bodyChunks[1];
                                    bodyChunk7.vel.y = bodyChunk7.vel.y + 4f * num;
                                    player.jumpBoost = 6f;
                                    if (num2 != 0 && ((Creature)player).bodyChunks[0].pos.x > ((Creature)player).bodyChunks[1].pos.x == num2 > 0)
                                    {
                                        BodyChunk bodyChunk8 = ((Creature)player).bodyChunks[0];
                                        bodyChunk8.vel.x = bodyChunk8.vel.x + (float)num2 * num3 * num;
                                        BodyChunk bodyChunk9 = ((Creature)player).bodyChunks[1];
                                        bodyChunk9.vel.x = bodyChunk9.vel.x + (float)num2 * num3 * num;
                                        player.room.PlaySound((num3 != 9f) ? SoundID.Slugcat_Crouch_Jump : SoundID.Slugcat_Super_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                    }
                                }
                                if (((Creature)player).bodyChunks[1].onSlope != 0)
                                {
                                    if (num2 == -((Creature)player).bodyChunks[1].onSlope)
                                    {
                                        BodyChunk bodyChunk10 = ((Creature)player).bodyChunks[1];
                                        bodyChunk10.vel.x = bodyChunk10.vel.x + (float)((Creature)player).bodyChunks[1].onSlope * 8f * num;
                                    }
                                    else
                                    {
                                        BodyChunk bodyChunk11 = ((Creature)player).bodyChunks[0];
                                        bodyChunk11.vel.x = bodyChunk11.vel.x + (float)((Creature)player).bodyChunks[1].onSlope * 1.8f * num;
                                        BodyChunk bodyChunk12 = ((Creature)player).bodyChunks[1];
                                        bodyChunk12.vel.x = bodyChunk12.vel.x + (float)((Creature)player).bodyChunks[1].onSlope * 1.2f * num;
                                    }
                                }
                            }
                        }
                        else
                        {
                            player.jumpBoost = 0f;
                            if (player.input[0].x == 0)
                            {
                                if (player.input[0].y > 0)
                                {
                                    if (player.slowMovementStun < 1 && player.slideUpPole < 1)
                                    {
                                        player.Blink(7);
                                        for (int i = 0; i < 2; i++)
                                        {
                                            BodyChunk bodyChunk13 = ((Creature)player).bodyChunks[i];
                                            bodyChunk13.pos.y = bodyChunk13.pos.y + 4.5f;
                                            BodyChunk bodyChunk14 = ((Creature)player).bodyChunks[i];
                                            bodyChunk14.vel.y = bodyChunk14.vel.y + 2f;
                                        }
                                        player.slideUpPole = 17;
                                        player.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, ((Creature)player).mainBodyChunk, false, 0.8f, 1f);
                                    }
                                }
                                else
                                {
                                    player.animation = Player.AnimationIndex.None;
                                    ((Creature)player).bodyChunks[0].vel.y = 2f * num;
                                    if (player.input[0].y > -1)
                                    {
                                        ((Creature)player).bodyChunks[0].vel.x = 2f * (float)player.flipDirection * num;
                                    }
                                    player.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, ((Creature)player).mainBodyChunk, false, 0.3f, 1f);
                                }
                            }
                            else
                            {
                                player.animation = Player.AnimationIndex.None;
                                ((Creature)player).bodyChunks[0].vel.y = 9f * num; // # 8f -> 9f
                                ((Creature)player).bodyChunks[1].vel.y = 8f * num; // # 7f -> 8f
                                ((Creature)player).bodyChunks[0].vel.x = 9f * (float)player.flipDirection * num;  // # 6f -> 9f
                                ((Creature)player).bodyChunks[1].vel.x = 7f * (float)player.flipDirection * num; // # 5f -> 7f
                                player.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                            }
                        }
                    }
                    else if (player.input[0].x != 0)
                    {
                        player.WallJump(-player.input[0].x);
                    }
                    break;
                case Player.AnimationIndex.BellySlide:
                    if (player.whiplashJump || player.input[0].x == -player.rollDirection)
                    {
                        player.animation = Player.AnimationIndex.Flip;
                        player.standing = true;
                        player.room.AddObject(new ExplosionSpikes(player.room, ((Creature)player).bodyChunks[1].pos + new Vector2(0f, -((Creature)player).bodyChunks[1].rad), 8, 7f, 5f, 5.5f, 40f, new Color(1f, 1f, 1f, 0.5f)));
                        int num4 = 1;
                        for (int j = 1; j < 4; j++)
                        {
                            if (player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)(j * -(float)player.rollDirection) * 15f, 0f)).Solid || player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)(j * -(float)player.rollDirection) * 15f, 20f)).Solid)
                            {
                                break;
                            }
                            num4 = j;
                        }
                        ((Creature)player).bodyChunks[0].pos += new Vector2((float)player.rollDirection * -((float)num4 * 15f + 8f), 14f);
                        ((Creature)player).bodyChunks[1].pos += new Vector2((float)player.rollDirection * -((float)num4 * 15f + 2f), 0f);
                        ((Creature)player).bodyChunks[0].vel = new Vector2((float)player.rollDirection * -10.5f, 10f); // @ System completely changed: Using 1.5x multiplier on whiplash jump. -7f -> -10.5f
                        ((Creature)player).bodyChunks[1].vel = new Vector2((float)player.rollDirection * -10.5f, 11f); // @ System completely changed: Using 1.5x multiplier on whiplash jump. -7f -> -10.5f
                        player.rollDirection = -player.rollDirection;
                        player.flipFromSlide = true;
                        player.whiplashJump = false;
                        player.jumpBoost = 0f;
                        player.room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                        if (player.pickUpCandidate != null && player.CanIPickThisUp(player.pickUpCandidate) && (((Creature)player).grasps[0] == null || ((Creature)player).grasps[1] == null) && (player.Grabability(player.pickUpCandidate) == Player.ObjectGrabability.OneHand || player.Grabability(player.pickUpCandidate) == Player.ObjectGrabability.BigOneHand))
                        {
                            int graspUsed = (((Creature)player).grasps[0] != null) ? 1 : 0;
                            for (int k = 0; k < player.pickUpCandidate.grabbedBy.Count; k++)
                            {
                                player.pickUpCandidate.grabbedBy[k].grabber.GrabbedObjectSnatched(player.pickUpCandidate.grabbedBy[k].grabbed, player);
                                player.pickUpCandidate.grabbedBy[k].grabber.ReleaseGrasp(player.pickUpCandidate.grabbedBy[k].graspUsed);
                            }
                            player.SlugcatGrab(player.pickUpCandidate, graspUsed);
                            if (player.pickUpCandidate is PlayerCarryableItem)
                            {
                                (player.pickUpCandidate as PlayerCarryableItem).PickedUp(player);
                            }
                            if (player.pickUpCandidate.graphicsModule != null)
                            {
                                player.pickUpCandidate.graphicsModule.BringSpritesToFront();
                            }
                        }
                    }
                    else
                    {
                        ((Creature)player).bodyChunks[1].pos += new Vector2(5f * (float)player.rollDirection, 5f);
                        ((Creature)player).bodyChunks[0].pos = ((Creature)player).bodyChunks[1].pos + new Vector2(5f * (float)player.rollDirection, 5f);
                        ((Creature)player).bodyChunks[1].vel = new Vector2((float)player.rollDirection * 12f, 8.5f) * num * ((!player.longBellySlide) ? 1f : 1.2f); // # 9f -> 12f
                        ((Creature)player).bodyChunks[0].vel = new Vector2((float)player.rollDirection * 12f, 8.5f) * num * ((!player.longBellySlide) ? 1f : 1.2f); // # 9f -> 12f
                        player.animation = Player.AnimationIndex.RocketJump;
                        player.rocketJumpFromBellySlide = true;
                        player.room.PlaySound(SoundID.Slugcat_Rocket_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                        player.rollDirection = 0;
                    }
                    break;
                case Player.AnimationIndex.AntlerClimb:
                    {
                        player.animation = Player.AnimationIndex.None;
                        player.jumpBoost = 0f;
                        ((Creature)player).bodyChunks[0].vel = player.playerInAntlers.antlerChunk.vel;
                        if (!player.playerInAntlers.dangle)
                        {
                            ((Creature)player).bodyChunks[1].vel = player.playerInAntlers.antlerChunk.vel;
                        }
                        if (player.playerInAntlers.dangle)
                        {
                            if (player.input[0].x == 0)
                            {
                                BodyChunk bodyChunk15 = ((Creature)player).bodyChunks[0];
                                bodyChunk15.vel.y = bodyChunk15.vel.y + 3f;
                                BodyChunk bodyChunk16 = ((Creature)player).bodyChunks[1];
                                bodyChunk16.vel.y = bodyChunk16.vel.y - 3f;
                                player.standing = true;
                                player.room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                            }
                            else
                            {
                                BodyChunk bodyChunk17 = ((Creature)player).bodyChunks[1];
                                bodyChunk17.vel.y = bodyChunk17.vel.y + 4f;
                                BodyChunk bodyChunk18 = ((Creature)player).bodyChunks[1];
                                bodyChunk18.vel.x = bodyChunk18.vel.x + 2f * (float)player.input[0].x;
                                BodyChunk bodyChunk19 = ((Creature)player).bodyChunks[0];
                                bodyChunk19.vel.y = bodyChunk19.vel.y + 6f;
                                BodyChunk bodyChunk20 = ((Creature)player).bodyChunks[0];
                                bodyChunk20.vel.x = bodyChunk20.vel.x + 3f * (float)player.input[0].x;
                                player.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, ((Creature)player).mainBodyChunk, false, 0.15f, 1f);
                            }
                        }
                        else if (player.input[0].x == 0)
                        {
                            if (player.input[0].y > 0)
                            {
                                BodyChunk bodyChunk21 = ((Creature)player).bodyChunks[0];
                                bodyChunk21.vel.y = bodyChunk21.vel.y + 4f * num;
                                BodyChunk bodyChunk22 = ((Creature)player).bodyChunks[1];
                                bodyChunk22.vel.y = bodyChunk22.vel.y + 3f * num;
                                player.jumpBoost = 8f;
                                player.room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                player.standing = true;
                            }
                            else
                            {
                                ((Creature)player).bodyChunks[0].vel.y = 3f;
                                ((Creature)player).bodyChunks[1].vel.y = -3f;
                                player.standing = true;
                                player.room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                            }
                        }
                        else
                        {
                            BodyChunk bodyChunk23 = ((Creature)player).bodyChunks[0];
                            bodyChunk23.vel.y = bodyChunk23.vel.y + 8f * num;
                            BodyChunk bodyChunk24 = ((Creature)player).bodyChunks[1];
                            bodyChunk24.vel.y = bodyChunk24.vel.y + 7f * num;
                            BodyChunk bodyChunk25 = ((Creature)player).bodyChunks[0];
                            bodyChunk25.vel.x = bodyChunk25.vel.x + 6f * (float)player.input[0].x * num;
                            BodyChunk bodyChunk26 = ((Creature)player).bodyChunks[1];
                            bodyChunk26.vel.x = bodyChunk26.vel.x + 5f * (float)player.input[0].x * num;
                            player.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                        }
                        Vector2 vector = ((Creature)player).bodyChunks[0].vel - player.playerInAntlers.antlerChunk.vel + (((Creature)player).bodyChunks[1].vel - player.playerInAntlers.antlerChunk.vel) * ((!player.playerInAntlers.dangle) ? 1f : 0f);
                        vector -= RWCustom.Custom.DirVec(((Creature)player).mainBodyChunk.pos, player.playerInAntlers.deer.mainBodyChunk.pos) * vector.magnitude;
                        vector.x *= 0.1f;
                        vector = Vector2.ClampMagnitude(vector, 10f);
                        player.playerInAntlers.antlerChunk.vel -= vector * 1.2f;
                        player.playerInAntlers.deer.mainBodyChunk.vel -= vector * 0.25f;
                        player.playerInAntlers.playerDisconnected = true;
                        player.playerInAntlers = null;
                        break;
                    }
                case Player.AnimationIndex.ZeroGSwim:
                    break;
                case Player.AnimationIndex.ZeroGPoleGrab:
                    break;
            }
        }

        private void HookPlayerLungUpdate(On.Player.orig_LungUpdate orig, Player player)
        {
            if (base.IsMe(player) && player.room.game.globalRain.deathRain == null)
            {
                player.airInLungs = 1f;
            }
            orig.Invoke(player);
        }

        private void HookPlayerWallJump(On.Player.orig_WallJump orig, Player player, int direction)
        {
            if (!base.IsMe(player))
            {
                orig.Invoke(player, direction);
                return;
            }
            float num = Mathf.Lerp(1f, 1.15f, player.Adrenaline);
            if (player.exhausted)
            {
                num *= 1f - 0.5f * player.aerobicLevel;
            }
            bool flag = player.input[0].x != 0 && ((Creature)player).bodyChunks[0].ContactPoint.x == player.input[0].x && ((Creature)player).IsTileSolid(0, player.input[0].x, 0) && !((Creature)player).IsTileSolid(0, player.input[0].x, 1);
            if (((Creature)player).IsTileSolid(1, 0, -1) || ((Creature)player).IsTileSolid(0, 0, -1) || ((Creature)player).bodyChunks[1].submersion > 0.1f || flag)
            {
                if (((Creature)player).bodyChunks[1].ContactPoint.y > -1 && ((Creature)player).bodyChunks[0].ContactPoint.y > -1 && ((Creature)player).Submersion == 0f)
                {
                    num *= 0.7f;
                }
                ((Creature)player).bodyChunks[0].vel.y = 9f * num; // # 8f -> 9f
                ((Creature)player).bodyChunks[1].vel.y = 8f * num; // # 7f -> 8f
                BodyChunk bodyChunk = ((Creature)player).bodyChunks[0];
                bodyChunk.pos.y = bodyChunk.pos.y + 10f * Mathf.Min(1f, num);
                BodyChunk bodyChunk2 = ((Creature)player).bodyChunks[1];
                bodyChunk2.pos.y = bodyChunk2.pos.y + 10f * Mathf.Min(1f, num);
                player.room.PlaySound(SoundID.Slugcat_Normal_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                player.jumpBoost = 0f;
            }
            else
            {
                ((Creature)player).bodyChunks[0].vel.y = 10f * num; // # 8f -> 10f
                ((Creature)player).bodyChunks[1].vel.y = 9f * num; // # 7f -> 9f
                ((Creature)player).bodyChunks[0].vel.x = 9f * num * (float)direction; // # 6f -> 9f
                ((Creature)player).bodyChunks[1].vel.x = 7f * num * (float)direction; // # 5f -> 7f
                player.room.PlaySound(SoundID.Slugcat_Wall_Jump, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                player.standing = true;
                player.jumpBoost = 4f; // # 0f -> 4f
                player.jumpStun = 8 * direction;
            }
            player.canWallJump = 0;
        }

        private void HookPlayerMovementUpdate(On.Player.orig_MovementUpdate orig, Player player, bool eu)
        {
            player.DirectIntoHoles();
            if (player.rocketJumpFromBellySlide && player.animation != Player.AnimationIndex.RocketJump)
            {
                player.rocketJumpFromBellySlide = false;
            }
            if (player.flipFromSlide && player.animation != Player.AnimationIndex.Flip)
            {
                player.flipFromSlide = false;
            }
            if (player.whiplashJump && player.animation != Player.AnimationIndex.BellySlide)
            {
                player.whiplashJump = false;
            }
            int num = player.input[0].x;
            if (player.jumpStun != 0)
            {
                num = player.jumpStun / Mathf.Abs(player.jumpStun);
            }
            player.lastFlipDirection = player.flipDirection;
            if (num != player.flipDirection && num != 0)
            {
                player.flipDirection = num;
            }
            int num2 = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (((Creature)player).IsTileSolid(j, RWCustom.Custom.eightDirections[i].x, RWCustom.Custom.eightDirections[i].y) && ((Creature)player).IsTileSolid(j, RWCustom.Custom.eightDirections[i + 4].x, RWCustom.Custom.eightDirections[i + 4].y))
                    {
                        num2++;
                    }
                }
            }
            bool flag = ((Creature)player).bodyChunks[1].onSlope == 0 && player.input[0].x == 0 && player.standing && ((Creature)player).stun < 1 && ((Creature)player).bodyChunks[1].ContactPoint.y == -1;
            if (player.feetStuckPos != null && !flag)
            {
                player.feetStuckPos = null;
            }
            else if (player.feetStuckPos == null && flag)
            {
                player.feetStuckPos = new Vector2?(new Vector2(((Creature)player).bodyChunks[1].pos.x, player.room.MiddleOfTile(player.room.GetTilePosition(((Creature)player).bodyChunks[1].pos)).y + -10f + ((Creature)player).bodyChunks[1].rad));
            }
            if (player.feetStuckPos != null)
            {
                player.feetStuckPos = new Vector2?(player.feetStuckPos.Value + new Vector2((((Creature)player).bodyChunks[1].pos.x - player.feetStuckPos.Value.x) * (1f - player.surfaceFriction), 0f));
                ((Creature)player).bodyChunks[1].pos = player.feetStuckPos.Value;
                if (!((Creature)player).IsTileSolid(1, 0, -1))
                {
                    bool flag2 = ((Creature)player).IsTileSolid(1, 1, -1) && !((Creature)player).IsTileSolid(1, 1, 0);
                    bool flag3 = ((Creature)player).IsTileSolid(1, -1, -1) && !((Creature)player).IsTileSolid(1, -1, 0);
                    if (flag3 && !flag2)
                    {
                        player.feetStuckPos = new Vector2?(player.feetStuckPos.Value + new Vector2(-1.6f * player.surfaceFriction, 0f));
                    }
                    else if (flag2 && !flag3)
                    {
                        player.feetStuckPos = new Vector2?(player.feetStuckPos.Value + new Vector2(1.6f * player.surfaceFriction, 0f));
                    }
                    else
                    {
                        player.feetStuckPos = null;
                    }
                }
            }
            if ((num2 > 1 && ((Creature)player).bodyChunks[0].onSlope == 0 && ((Creature)player).bodyChunks[1].onSlope == 0 && (!((Creature)player).IsTileSolid(0, 0, 0) || !((Creature)player).IsTileSolid(1, 0, 0))) || (((Creature)player).IsTileSolid(0, -1, 0) && ((Creature)player).IsTileSolid(0, 1, 0)) || (((Creature)player).IsTileSolid(1, -1, 0) && ((Creature)player).IsTileSolid(1, 1, 0)))
            {
                player.goIntoCorridorClimb++;
            }
            else
            {
                player.goIntoCorridorClimb = 0;
                bool flag4 = ((Creature)player).bodyChunks[0].ContactPoint.y == -1 || ((Creature)player).bodyChunks[1].ContactPoint.y == -1;
                player.bodyMode = Player.BodyModeIndex.Default;
                if (flag4)
                {
                    player.canJump = 5;
                    if (((Creature)player).bodyChunks[0].pos.y > ((Creature)player).bodyChunks[1].pos.y + 3f && !((Creature)player).IsTileSolid(1, 0, 1) && player.animation != Player.AnimationIndex.CrawlTurn && ((Creature)player).bodyChunks[0].ContactPoint.y > -1)
                    {
                        player.bodyMode = Player.BodyModeIndex.Stand;
                    }
                    else
                    {
                        player.bodyMode = Player.BodyModeIndex.Crawl;
                    }
                }
                else if (player.jumpBoost > 0f && (player.input[0].jmp || player.simulateHoldJumpButton > 0))
                {
                    player.jumpBoost -= 1.5f;
                    BodyChunk bodyChunk = ((Creature)player).bodyChunks[0];
                    bodyChunk.vel.y = bodyChunk.vel.y + (player.jumpBoost + 1f) * 0.3f;
                    BodyChunk bodyChunk2 = ((Creature)player).bodyChunks[1];
                    bodyChunk2.vel.y = bodyChunk2.vel.y + (player.jumpBoost + 1f) * 0.3f;
                }
                else
                {
                    player.jumpBoost = 0f;
                }
                if (((Creature)player).bodyChunks[0].ContactPoint.x != 0 && ((Creature)player).bodyChunks[0].ContactPoint.x == player.input[0].x)
                {
                    if (((Creature)player).bodyChunks[0].lastContactPoint.x != player.input[0].x)
                    {
                        player.room.PlaySound(SoundID.Slugcat_Enter_Wall_Slide, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                    }
                    player.bodyMode = Player.BodyModeIndex.WallClimb;
                }
                if (player.input[0].x != 0 && ((Creature)player).bodyChunks[0].pos.y > ((Creature)player).bodyChunks[1].pos.y && player.animation != Player.AnimationIndex.CrawlTurn && !((Creature)player).IsTileSolid(0, player.input[0].x, 0) && ((Creature)player).IsTileSolid(1, player.input[0].x, 0) && ((Creature)player).bodyChunks[1].ContactPoint.x == player.input[0].x)
                {
                    player.bodyMode = Player.BodyModeIndex.Crawl;
                    player.animation = Player.AnimationIndex.LedgeCrawl;
                }
                if (player.input[0].y == 1 && ((Creature)player).IsTileSolid(0, 0, 1) && !((Creature)player).IsTileSolid(1, 0, 1) && (((Creature)player).IsTileSolid(1, -1, 1) || ((Creature)player).IsTileSolid(1, 1, 1)))
                {
                    player.animation = Player.AnimationIndex.None;
                    BodyChunk bodyChunk3 = ((Creature)player).bodyChunks[1];
                    bodyChunk3.vel.y = bodyChunk3.vel.y + 2f * player.room.gravity;
                    BodyChunk bodyChunk4 = ((Creature)player).bodyChunks[0];
                    bodyChunk4.vel.x = bodyChunk4.vel.x - (((Creature)player).bodyChunks[0].pos.x - ((Creature)player).bodyChunks[1].pos.x) * 0.25f * player.room.gravity;
                    BodyChunk bodyChunk5 = ((Creature)player).bodyChunks[0];
                    bodyChunk5.vel.y = bodyChunk5.vel.y - player.room.gravity;
                }
            }
            if (player.input[0].y > 0 && player.input[0].x == 0 && player.bodyMode == Player.BodyModeIndex.Default && ((Creature)player).firstChunk.pos.y - ((Creature)player).firstChunk.lastPos.y < 2f && ((Creature)player).bodyChunks[1].ContactPoint.y == 0 && !((Creature)player).IsTileSolid(0, 0, 1) && ((Creature)player).IsTileSolid(0, -1, 1) && ((Creature)player).IsTileSolid(0, 1, 1) && !((Creature)player).IsTileSolid(1, -1, 0) && !((Creature)player).IsTileSolid(1, 1, 0) && player.room.GetTilePosition(((Creature)player).firstChunk.pos) == player.room.GetTilePosition(((Creature)player).bodyChunks[1].pos) + new RWCustom.IntVector2(0, 1) && Mathf.Abs(((Creature)player).firstChunk.pos.x - player.room.MiddleOfTile(((Creature)player).firstChunk.pos).x) < 5f && player.room.gravity > 0f)
            {
                ((Creature)player).firstChunk.pos.x = player.room.MiddleOfTile(((Creature)player).firstChunk.pos).x;
                BodyChunk firstChunk = ((Creature)player).firstChunk;
                firstChunk.pos.y = firstChunk.pos.y + 1f;
                BodyChunk firstChunk2 = ((Creature)player).firstChunk;
                firstChunk2.vel.y = firstChunk2.vel.y + 1f;
                BodyChunk bodyChunk6 = ((Creature)player).bodyChunks[1];
                bodyChunk6.vel.y = bodyChunk6.vel.y + 1f;
                BodyChunk bodyChunk7 = ((Creature)player).bodyChunks[1];
                bodyChunk7.pos.y = bodyChunk7.pos.y + 1f;
            }
            if (player.input[0].y == 1 && player.input[1].y != 1)
            {
                if (((Creature)player).bodyChunks[1].onSlope == 0 || !((Creature)player).IsTileSolid(0, 0, 1))
                {
                    player.standing = true;
                }
            }
            else if (player.input[0].y == -1 && player.input[1].y != -1)
            {
                if (player.standing && player.bodyMode == Player.BodyModeIndex.Stand)
                {
                    player.room.PlaySound(SoundID.Slugcat_Down_On_Fours, ((Creature)player).mainBodyChunk);
                }
                player.standing = false;
            }
            if (player.room.gravity > 0f && player.animation == Player.AnimationIndex.ZeroGPoleGrab)
            {
                player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                if (player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam)
                {
                    player.animation = Player.AnimationIndex.HangFromBeam;
                }
                else
                {
                    player.animation = Player.AnimationIndex.ClimbOnBeam;
                }
            }
            if (player.goIntoCorridorClimb > 2 && !player.corridorDrop)
            {
                player.bodyMode = Player.BodyModeIndex.CorridorClimb;
                player.animation = ((player.corridorTurnDir == null) ? Player.AnimationIndex.None : Player.AnimationIndex.CorridorTurn);
            }
            if (player.corridorDrop)
            {
                player.bodyMode = Player.BodyModeIndex.Default;
                player.animation = Player.AnimationIndex.None;
                if (player.input[0].y >= 0 || player.goIntoCorridorClimb < 2)
                {
                    player.corridorDrop = false;
                }
                if (((Creature)player).bodyChunks[0].pos.y < ((Creature)player).bodyChunks[1].pos.y)
                {
                    for (int k = 0; k < RWCustom.Custom.IntClamp((int)(((Creature)player).bodyChunks[0].vel.y * -0.3f), 1, 10); k++)
                    {
                        if (((Creature)player).IsTileSolid(0, 0, -k))
                        {
                            player.corridorDrop = false;
                            break;
                        }
                    }
                }
            }
            if (player.bodyMode != Player.BodyModeIndex.WallClimb || ((Creature)player).bodyChunks[0].submersion == 1f)
            {
                bool flag5 = player.input[0].y < 0 || player.input[0].downDiagonal != 0;
                if ((((Creature)player).bodyChunks[0].submersion > 0.2f || ((Creature)player).bodyChunks[1].submersion > 0.2f) && player.bodyMode != Player.BodyModeIndex.CorridorClimb)
                {
                    if ((player.animation != Player.AnimationIndex.SurfaceSwim || flag5 || ((Creature)player).bodyChunks[0].pos.y < player.room.FloatWaterLevel(((Creature)player).bodyChunks[0].pos.x) - 80f) && ((Creature)player).bodyChunks[0].pos.y < player.room.FloatWaterLevel(((Creature)player).bodyChunks[0].pos.x) - ((!flag5) ? 30f : 10f) && ((Creature)player).bodyChunks[1].submersion > ((!flag5) ? 0.6f : -1f))
                    {
                        player.bodyMode = Player.BodyModeIndex.Swimming;
                        player.animation = Player.AnimationIndex.DeepSwim;
                    }
                    else if ((!((Creature)player).IsTileSolid(1, 0, -1) || ((Creature)player).bodyChunks[1].submersion == 1f) && player.animation != Player.AnimationIndex.BeamTip && player.animation != Player.AnimationIndex.ClimbOnBeam && player.animation != Player.AnimationIndex.GetUpOnBeam && player.animation != Player.AnimationIndex.GetUpToBeamTip && player.animation != Player.AnimationIndex.HangFromBeam && player.animation != Player.AnimationIndex.StandOnBeam && player.animation != Player.AnimationIndex.LedgeGrab && player.animation != Player.AnimationIndex.HangUnderVerticalBeam)
                    {
                        player.bodyMode = Player.BodyModeIndex.Swimming;
                        player.animation = Player.AnimationIndex.SurfaceSwim;
                    }
                }
            }
            if (player.room.gravity == 0f && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.animation != Player.AnimationIndex.VineGrab)
            {
                player.bodyMode = Player.BodyModeIndex.ZeroG;
                if (player.animation != Player.AnimationIndex.ZeroGSwim && player.animation != Player.AnimationIndex.ZeroGPoleGrab)
                {
                    player.animation = ((!player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam && !player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam) ? Player.AnimationIndex.ZeroGSwim : Player.AnimationIndex.ZeroGPoleGrab);
                }
            }
            if (player.playerInAntlers != null)
            {
                player.animation = Player.AnimationIndex.AntlerClimb;
            }
            if (player.tubeWorm != null)
            {
                bool flag6 = true;
                int num3 = 0;
                while (num3 < ((Creature)player).grasps.Length && flag6)
                {
                    if (((Creature)player).grasps[num3] != null && ((Creature)player).grasps[num3].grabbed as TubeWorm == player.tubeWorm)
                    {
                        flag6 = false;
                    }
                    num3++;
                }
                if (flag6)
                {
                    player.tubeWorm = null;
                }
            }
            if (player.tubeWorm != null && player.tubeWorm.tongues[0].Attached && player.bodyMode == Player.BodyModeIndex.Default && ((Creature)player).bodyChunks[1].ContactPoint.y >= 0 && (player.animation == Player.AnimationIndex.GrapplingSwing || player.animation == Player.AnimationIndex.None))
            {
                player.animation = Player.AnimationIndex.GrapplingSwing;
            }
            else if (player.animation == Player.AnimationIndex.GrapplingSwing)
            {
                player.animation = Player.AnimationIndex.None;
            }
            if (player.vineGrabDelay > 0)
            {
                player.vineGrabDelay--;
            }
            if (player.animation != Player.AnimationIndex.VineGrab && player.vineGrabDelay == 0 && player.room.climbableVines != null)
            {
                if (player.room.gravity > 0f && (player.wantToGrab > 0 || player.input[0].y > 0))
                {
                    int num4 = RWCustom.Custom.IntClamp((int)(Vector2.Distance(((Creature)player).mainBodyChunk.lastPos, ((Creature)player).mainBodyChunk.pos) / 5f), 1, 10);
                    for (int l = 0; l < num4; l++)
                    {
                        Vector2 pos = Vector2.Lerp(((Creature)player).mainBodyChunk.lastPos, ((Creature)player).mainBodyChunk.pos, (num4 <= 1) ? 0f : ((float)l / (float)(num4 - 1)));
                        ClimbableVinesSystem.VinePosition vinePosition = player.room.climbableVines.VineOverlap(pos, ((Creature)player).mainBodyChunk.rad);
                        if (vinePosition != null)
                        {
                            if (player.room.climbableVines.GetVineObject(vinePosition) is CoralBrain.CoralNeuron)
                            {
                                player.room.PlaySound(SoundID.Grab_Neuron, ((Creature)player).mainBodyChunk);
                            }
                            else if (player.room.climbableVines.GetVineObject(vinePosition) is CoralBrain.CoralStem)
                            {
                                player.room.PlaySound(SoundID.Grab_Coral_Stem, ((Creature)player).mainBodyChunk);
                            }
                            else if (player.room.climbableVines.GetVineObject(vinePosition) is DaddyCorruption.ClimbableCorruptionTube)
                            {
                                player.room.PlaySound(SoundID.Grab_Corruption_Tube, ((Creature)player).mainBodyChunk);
                            }
                            player.animation = Player.AnimationIndex.VineGrab;
                            player.vinePos = vinePosition;
                            player.wantToGrab = 0;
                            break;
                        }
                    }
                }
                else if (player.animation != Player.AnimationIndex.VineGrab && (player.input[0].x != 0 || player.input[0].y != 0) && player.room.gravity == 0f)
                {
                    ClimbableVinesSystem.VinePosition vinePosition2 = player.room.climbableVines.VineOverlap(((Creature)player).mainBodyChunk.pos, ((Creature)player).mainBodyChunk.rad);
                    if (vinePosition2 != null)
                    {
                        if (player.room.climbableVines.GetVineObject(vinePosition2) is CoralBrain.CoralNeuron)
                        {
                            player.room.PlaySound(SoundID.Grab_Neuron, ((Creature)player).mainBodyChunk);
                        }
                        else if (player.room.climbableVines.GetVineObject(vinePosition2) is CoralBrain.CoralStem)
                        {
                            player.room.PlaySound(SoundID.Grab_Coral_Stem, ((Creature)player).mainBodyChunk);
                        }
                        else if (player.room.climbableVines.GetVineObject(vinePosition2) is DaddyCorruption.ClimbableCorruptionTube)
                        {
                            player.room.PlaySound(SoundID.Grab_Corruption_Tube, ((Creature)player).mainBodyChunk);
                        }
                        player.animation = Player.AnimationIndex.VineGrab;
                        player.vinePos = vinePosition2;
                        player.wantToGrab = 0;
                    }
                }
            }
            player.dynamicRunSpeed[0] = 7f; // # 3.6f -> 7f
            player.dynamicRunSpeed[1] = 7f; // # 3.6f -> 7f
            float num5 = 3.6f; // # 2.4f -> 3.6f
            player.UpdateAnimation();
            if (((Creature)player).bodyChunks[0].ContactPoint.x == player.input[0].x && player.input[0].x != 0 && ((Creature)player).bodyChunks[0].pos.y > player.room.MiddleOfTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos)).y && (player.bodyMode == Player.BodyModeIndex.Default || player.bodyMode == Player.BodyModeIndex.WallClimb) && !((Creature)player).IsTileSolid(0, -player.input[0].x, 0) && !((Creature)player).IsTileSolid(0, 0, -2) && !((Creature)player).IsTileSolid(0, player.input[0].x, 1))
            {
                player.animation = Player.AnimationIndex.LedgeGrab;
                player.bodyMode = Player.BodyModeIndex.Default;
            }
            if (player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                player.crawlTurnDelay++;
            }
            else
            {
                player.crawlTurnDelay = 0;
            }
            if (player.standing && ((Creature)player).IsTileSolid(1, 0, 1))
            {
                player.standing = false;
            }
            if (player.input[0].y > 0 && player.input[1].y == 0 && !player.room.GetTile(((Creature)player).bodyChunks[1].pos).verticalBeam && player.room.GetTile(((Creature)player).bodyChunks[1].pos + new Vector2(0f, -20f)).verticalBeam)
            {
                player.animation = Player.AnimationIndex.BeamTip;
                ((Creature)player).bodyChunks[1].vel.x = 0f;
                ((Creature)player).bodyChunks[1].vel.y = 0f;
                player.wantToGrab = -1;
            }
            player.UpdateBodyMode();
            if (player.rollDirection != 0)
            {
                player.rollCounter++;
                num = player.rollDirection;
                player.bodyChunkConnections[0].distance = 10f;
                if (player.bodyMode != Player.BodyModeIndex.Default || player.rollCounter > 200)
                {
                    player.rollCounter = 0;
                    player.rollDirection = 0;
                }
            }
            else
            {
                player.bodyChunkConnections[0].distance = 17f;
            }
            player.bodyChunkConnections[0].type = ((player.corridorTurnDir == null) ? PhysicalObject.BodyChunkConnection.Type.Normal : PhysicalObject.BodyChunkConnection.Type.Pull);
            player.wantToGrab = ((player.input[0].y <= 0) ? 0 : 1);
            if (player.wantToGrab > 0 && player.noGrabCounter == 0 && (player.bodyMode == Player.BodyModeIndex.Default || player.bodyMode == Player.BodyModeIndex.WallClimb || player.bodyMode == Player.BodyModeIndex.Stand || player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || player.bodyMode == Player.BodyModeIndex.Swimming) && (player.timeSinceInCorridorMode >= 20 || ((Creature)player).bodyChunks[1].pos.y <= ((Creature)player).firstChunk.pos.y || player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos).x != player.room.GetTilePosition(((Creature)player).bodyChunks[1].pos).x) && player.animation != Player.AnimationIndex.ClimbOnBeam && player.animation != Player.AnimationIndex.HangFromBeam && player.animation != Player.AnimationIndex.GetUpOnBeam && player.animation != Player.AnimationIndex.DeepSwim && player.animation != Player.AnimationIndex.HangUnderVerticalBeam && player.animation != Player.AnimationIndex.GetUpToBeamTip && player.animation != Player.AnimationIndex.VineGrab)
            {
                int x = player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos).x;
                int num6 = player.room.GetTilePosition(((Creature)player).bodyChunks[0].lastPos).y;
                int num7 = player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos).y;
                if (num7 > num6)
                {
                    int num8 = num6;
                    num6 = num7;
                    num7 = num8;
                }
                for (int m = num6; m >= num7; m--)
                {
                    if (player.room.GetTile(x, m).horizontalBeam)
                    {
                        player.animation = Player.AnimationIndex.HangFromBeam;
                        player.room.PlaySound(SoundID.Slugcat_Grab_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                        ((Creature)player).bodyChunks[0].vel.y = 0f;
                        BodyChunk bodyChunk8 = ((Creature)player).bodyChunks[1];
                        bodyChunk8.vel.y = bodyChunk8.vel.y * 0.25f;
                        ((Creature)player).bodyChunks[0].pos.y = player.room.MiddleOfTile(new RWCustom.IntVector2(x, m)).y;
                        break;
                    }
                }
                player.GrabVerticalPole();
                if (player.animation != Player.AnimationIndex.HangFromBeam && player.animation != Player.AnimationIndex.ClimbOnBeam && player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2(0f, 20f)).verticalBeam && !player.room.GetTile(((Creature)player).bodyChunks[0].pos).verticalBeam)
                {
                    ((Creature)player).bodyChunks[0].pos = player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos) + new Vector2(0f, 5f);
                    ((Creature)player).bodyChunks[0].vel *= 0f;
                    ((Creature)player).bodyChunks[1].vel = Vector2.ClampMagnitude(((Creature)player).bodyChunks[1].vel, 9f);
                    player.animation = Player.AnimationIndex.HangUnderVerticalBeam;
                }
            }
            bool flag7 = false;
            if (player.bodyMode != Player.BodyModeIndex.CorridorClimb)
            {
                flag7 = true;
            }
            if (player.animation == Player.AnimationIndex.ClimbOnBeam || player.animation == Player.AnimationIndex.HangFromBeam || player.animation == Player.AnimationIndex.GetUpOnBeam || player.animation == Player.AnimationIndex.LedgeGrab || player.animation == Player.AnimationIndex.GrapplingSwing || player.animation == Player.AnimationIndex.AntlerClimb)
            {
                flag7 = false;
            }
            if (((Creature)player).grasps[0] != null && player.HeavyCarry(((Creature)player).grasps[0].grabbed))
            {
                float num9 = 1f + Mathf.Max(0f, ((Creature)player).grasps[0].grabbed.TotalMass - 0.2f);
                if (((Creature)player).grasps[0].grabbed is Cicada)
                {
                    if (player.bodyMode == Player.BodyModeIndex.Default && player.animation == Player.AnimationIndex.None)
                    {
                        BodyChunk mainBodyChunk = ((Creature)player).mainBodyChunk;
                        mainBodyChunk.vel.y = mainBodyChunk.vel.y + (((Creature)player).grasps[0].grabbed as Cicada).LiftPlayerPower * 1.2f;
                        BodyChunk bodyChunk9 = ((Creature)player).bodyChunks[1];
                        bodyChunk9.vel.y = bodyChunk9.vel.y + (((Creature)player).grasps[0].grabbed as Cicada).LiftPlayerPower * 0.25f;
                        (((Creature)player).grasps[0].grabbed as Cicada).currentlyLiftingPlayer = true;
                        if ((((Creature)player).grasps[0].grabbed as Cicada).LiftPlayerPower > 0.6666667f)
                        {
                            player.standing = false;
                        }
                    }
                    else
                    {
                        BodyChunk mainBodyChunk2 = ((Creature)player).mainBodyChunk;
                        mainBodyChunk2.vel.y = mainBodyChunk2.vel.y + (((Creature)player).grasps[0].grabbed as Cicada).LiftPlayerPower * 0.5f;
                        (((Creature)player).grasps[0].grabbed as Cicada).currentlyLiftingPlayer = false;
                    }
                    if (((Creature)player).bodyChunks[1].ContactPoint.y < 0 && ((Creature)player).bodyChunks[1].lastContactPoint.y == 0 && (((Creature)player).grasps[0].grabbed as Cicada).LiftPlayerPower > 0.333333343f)
                    {
                        player.standing = true;
                    }
                    num9 = 1f + Mathf.Max(0f, ((Creature)player).grasps[0].grabbed.TotalMass - 0.2f) * 1.5f;
                    num9 = Mathf.Lerp(num9, 1f, Mathf.Pow(Mathf.InverseLerp(0.1f, 0.5f, (((Creature)player).grasps[0].grabbed as Cicada).LiftPlayerPower), 0.2f));
                }
                else if (player.Grabability(((Creature)player).grasps[0].grabbed) == Player.ObjectGrabability.Drag)
                {
                    if (player.bodyMode == Player.BodyModeIndex.Default || player.bodyMode == Player.BodyModeIndex.CorridorClimb || player.bodyMode == Player.BodyModeIndex.Stand || player.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        num9 = 1f;
                    }
                    if (player.room.aimap.getAItile(((Creature)player).mainBodyChunk.pos).narrowSpace)
                    {
                        ((Creature)player).grasps[0].grabbedChunk.vel += player.input[0].IntVec.ToVector2().normalized * player.slugcatStats.corridorClimbSpeedFac * 4f / Mathf.Max(0.75f, ((Creature)player).grasps[0].grabbed.TotalMass);
                    }
                    for (int n = 0; n < ((Creature)player).grasps[0].grabbed.bodyChunks.Length; n++)
                    {
                        if (player.room.aimap.getAItile(((Creature)player).grasps[0].grabbed.bodyChunks[n].pos).narrowSpace)
                        {
                            ((Creature)player).grasps[0].grabbed.bodyChunks[n].vel *= 0.8f;
                            BodyChunk bodyChunk10 = ((Creature)player).grasps[0].grabbed.bodyChunks[n];
                            bodyChunk10.vel.y = bodyChunk10.vel.y + player.room.gravity * ((Creature)player).grasps[0].grabbed.gravity * 0.85f;
                            ((Creature)player).grasps[0].grabbed.bodyChunks[n].vel += player.input[0].IntVec.ToVector2().normalized * player.slugcatStats.corridorClimbSpeedFac * 1.5f / ((float)((Creature)player).grasps[0].grabbed.bodyChunks.Length * Mathf.Max(1f, (((Creature)player).grasps[0].grabbed.TotalMass + 1f) / 2f));
                            ((Creature)player).grasps[0].grabbed.bodyChunks[n].pos += player.input[0].IntVec.ToVector2().normalized * player.slugcatStats.corridorClimbSpeedFac * 1.1f / ((float)((Creature)player).grasps[0].grabbed.bodyChunks.Length * Mathf.Max(1f, (((Creature)player).grasps[0].grabbed.TotalMass + 2f) / 3f));
                        }
                    }
                }
                if (player.shortcutDelay < 1 && player.enteringShortCut == null && (player.input[0].x == 0 || player.input[0].y == 0) && (player.input[0].x != 0 || player.input[0].y != 0))
                {
                    for (int num10 = 0; num10 < ((Creature)player).grasps[0].grabbed.bodyChunks.Length; num10++)
                    {
                        if (player.room.GetTile(player.room.GetTilePosition(((Creature)player).grasps[0].grabbed.bodyChunks[num10].pos) + player.input[0].IntVec).Terrain == Room.Tile.TerrainType.ShortcutEntrance && player.room.ShorcutEntranceHoleDirection(player.room.GetTilePosition(((Creature)player).grasps[0].grabbed.bodyChunks[num10].pos) + player.input[0].IntVec) == new RWCustom.IntVector2(-player.input[0].x, -player.input[0].y))
                        {
                            ShortcutData.Type shortCutType = player.room.shortcutData(player.room.GetTilePosition(((Creature)player).grasps[0].grabbed.bodyChunks[num10].pos) + player.input[0].IntVec).shortCutType;
                            if (shortCutType == ShortcutData.Type.RoomExit || shortCutType == ShortcutData.Type.Normal)
                            {
                                player.enteringShortCut = new RWCustom.IntVector2?(player.room.GetTilePosition(((Creature)player).grasps[0].grabbed.bodyChunks[num10].pos) + player.input[0].IntVec);
                                Debug.Log("player pulled into shortcut by carried object");
                                break;
                            }
                        }
                    }
                }
                player.dynamicRunSpeed[0] /= num9;
                player.dynamicRunSpeed[1] /= num9;
            }
            player.dynamicRunSpeed[0] *= Mathf.Lerp(1f, 1.5f, player.Adrenaline);
            player.dynamicRunSpeed[1] *= Mathf.Lerp(1f, 1.5f, player.Adrenaline);
            num5 *= Mathf.Lerp(1f, 1.2f, player.Adrenaline);
            if (flag7 && (player.dynamicRunSpeed[0] > 0f || player.dynamicRunSpeed[1] > 0f))
            {
                if (player.slowMovementStun > 0)
                {
                    player.dynamicRunSpeed[0] *= 0.5f + 0.5f * Mathf.InverseLerp(10f, 0f, (float)player.slowMovementStun);
                    player.dynamicRunSpeed[1] *= 0.5f + 0.5f * Mathf.InverseLerp(10f, 0f, (float)player.slowMovementStun);
                    num5 *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0f, (float)player.slowMovementStun);
                }
                if (player.bodyMode == Player.BodyModeIndex.Default && ((Creature)player).bodyChunks[0].ContactPoint.x == 0 && ((Creature)player).bodyChunks[0].ContactPoint.y == 0 && ((Creature)player).bodyChunks[1].ContactPoint.x == 0 && ((Creature)player).bodyChunks[1].ContactPoint.y == 0)
                {
                    num5 *= player.room.gravity;
                }
                for (int num11 = 0; num11 < 2; num11++)
                {
                    if (num < 0)
                    {
                        float num12 = num5 * player.surfaceFriction;
                        if (((Creature)player).bodyChunks[num11].vel.x - num12 < -player.dynamicRunSpeed[num11])
                        {
                            num12 = player.dynamicRunSpeed[num11] + ((Creature)player).bodyChunks[num11].vel.x;
                        }
                        if (num12 > 0f)
                        {
                            BodyChunk bodyChunk11 = ((Creature)player).bodyChunks[num11];
                            bodyChunk11.vel.x = bodyChunk11.vel.x - num12;
                        }
                    }
                    else if (num > 0)
                    {
                        float num13 = num5 * player.surfaceFriction;
                        if (((Creature)player).bodyChunks[num11].vel.x + num13 > player.dynamicRunSpeed[num11])
                        {
                            num13 = player.dynamicRunSpeed[num11] - ((Creature)player).bodyChunks[num11].vel.x;
                        }
                        if (num13 > 0f)
                        {
                            BodyChunk bodyChunk12 = ((Creature)player).bodyChunks[num11];
                            bodyChunk12.vel.x = bodyChunk12.vel.x + num13;
                        }
                    }
                    if (((Creature)player).bodyChunks[0].ContactPoint.y != 0 || ((Creature)player).bodyChunks[1].ContactPoint.y != 0)
                    {
                        float num14 = 0f;
                        if (player.input[0].x != 0)
                        {
                            num14 = Mathf.Clamp(((Creature)player).bodyChunks[num11].vel.x, -player.dynamicRunSpeed[num11], player.dynamicRunSpeed[num11]);
                        }
                        BodyChunk bodyChunk13 = ((Creature)player).bodyChunks[num11];
                        bodyChunk13.vel.x = bodyChunk13.vel.x + (num14 - ((Creature)player).bodyChunks[num11].vel.x) * Mathf.Pow(player.surfaceFriction, 1.5f);
                    }
                }
            }
            int num15 = 0;
            if (player.superLaunchJump > 0 && player.killSuperLaunchJumpCounter < 1)
            {
                num15 = 1;
            }
            if (player.bodyMode == Player.BodyModeIndex.Crawl && ((Creature)player).bodyChunks[0].ContactPoint.y < 0 && ((Creature)player).bodyChunks[1].ContactPoint.y < 0)
            {
                if (player.input[0].x == 0 && player.input[0].y == 0)
                {
                    num15 = 0;
                    player.wantToJump = 0;
                    if (player.input[0].jmp)
                    {
                        if (player.superLaunchJump < 20)
                        {
                            player.superLaunchJump++;
                            if (player.Adrenaline == 1f && player.superLaunchJump < 6)
                            {
                                player.superLaunchJump = 6;
                            }
                        }
                        else
                        {
                            player.killSuperLaunchJumpCounter = 15;
                        }
                    }
                }
                if (!player.input[0].jmp && player.input[1].jmp)
                {
                    player.wantToJump = 1;
                }
            }
            if (player.killSuperLaunchJumpCounter > 0)
            {
                player.killSuperLaunchJumpCounter--;
            }
            if (player.simulateHoldJumpButton > 0)
            {
                player.simulateHoldJumpButton--;
            }
            if (player.canJump > 0 && player.wantToJump > 0)
            {
                player.canJump = 0;
                player.wantToJump = 0;
                player.Jump();
            }
            else if (player.canWallJump != 0 && player.wantToJump > 0 && player.input[0].x != -Math.Sign(player.canWallJump))
            {
                player.WallJump(Math.Sign(player.canWallJump));
                player.wantToJump = 0;
            }
            else if (player.jumpChunkCounter > 0 && player.wantToJump > 0)
            {
                player.jumpChunkCounter = -5;
                player.wantToJump = 0;
                player.JumpOnChunk();
            }
            if (player.Adrenaline > 0f)
            {
                float num16 = 16f * player.Adrenaline; // #8f -> 16f
                if (player.input[0].x < 0)
                {
                    if (!((Creature)player).IsTileSolid(0, -1, 0) && player.directionBoosts[0] == 1f)
                    {
                        player.directionBoosts[0] = 0f;
                        BodyChunk mainBodyChunk3 = ((Creature)player).mainBodyChunk;
                        mainBodyChunk3.vel.x = mainBodyChunk3.vel.x - num16;
                        BodyChunk bodyChunk14 = ((Creature)player).bodyChunks[1];
                        bodyChunk14.vel.x = bodyChunk14.vel.x + num16 / 3f;
                    }
                }
                else if (player.directionBoosts[0] == 0f)
                {
                    player.directionBoosts[0] = 0.01f;
                }
                if (player.input[0].x > 0)
                {
                    if (!((Creature)player).IsTileSolid(0, 1, 0) && player.directionBoosts[1] == 1f)
                    {
                        player.directionBoosts[1] = 0f;
                        BodyChunk mainBodyChunk4 = ((Creature)player).mainBodyChunk;
                        mainBodyChunk4.vel.x = mainBodyChunk4.vel.x + num16;
                        BodyChunk bodyChunk15 = ((Creature)player).bodyChunks[1];
                        bodyChunk15.vel.x = bodyChunk15.vel.x - num16 / 3f;
                    }
                }
                else if (player.directionBoosts[1] == 0f)
                {
                    player.directionBoosts[1] = 0.01f;
                }
                if (player.input[0].y < 0)
                {
                    if (!((Creature)player).IsTileSolid(0, 0, -1) && player.directionBoosts[2] == 1f)
                    {
                        player.directionBoosts[2] = 0f;
                        BodyChunk mainBodyChunk5 = ((Creature)player).mainBodyChunk;
                        mainBodyChunk5.vel.y = mainBodyChunk5.vel.y - num16;
                        BodyChunk bodyChunk16 = ((Creature)player).bodyChunks[1];
                        bodyChunk16.vel.y = bodyChunk16.vel.y + num16 / 3f;
                    }
                }
                else if (player.directionBoosts[2] == 0f)
                {
                    player.directionBoosts[2] = 0.01f;
                }
                if (player.input[0].y > 0)
                {
                    if (!((Creature)player).IsTileSolid(0, 0, 1) && player.directionBoosts[3] == 1f)
                    {
                        player.directionBoosts[3] = 0f;
                        BodyChunk mainBodyChunk6 = ((Creature)player).mainBodyChunk;
                        mainBodyChunk6.vel.y = mainBodyChunk6.vel.y + num16;
                        BodyChunk bodyChunk17 = ((Creature)player).bodyChunks[1];
                        bodyChunk17.vel.y = bodyChunk17.vel.y - num16;
                    }
                }
                else if (player.directionBoosts[3] == 0f)
                {
                    player.directionBoosts[3] = 0.01f;
                }
            }
            player.superLaunchJump -= num15;
            if (player.shortcutDelay < 1)
            {
                for (int num17 = 0; num17 < 2; num17++)
                {
                    if (player.enteringShortCut == null && player.room.GetTile(((Creature)player).bodyChunks[num17].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance && player.room.shortcutData(player.room.GetTilePosition(((Creature)player).bodyChunks[num17].pos)).shortCutType != ShortcutData.Type.DeadEnd && player.room.shortcutData(player.room.GetTilePosition(((Creature)player).bodyChunks[num17].pos)).shortCutType != ShortcutData.Type.CreatureHole && player.room.shortcutData(player.room.GetTilePosition(((Creature)player).bodyChunks[num17].pos)).shortCutType != ShortcutData.Type.NPCTransportation)
                    {
                        RWCustom.IntVector2 intVector = player.room.ShorcutEntranceHoleDirection(player.room.GetTilePosition(((Creature)player).bodyChunks[num17].pos));
                        if (player.input[0].x == -intVector.x && player.input[0].y == -intVector.y)
                        {
                            player.enteringShortCut = new RWCustom.IntVector2?(player.room.GetTilePosition(((Creature)player).bodyChunks[num17].pos));
                        }
                    }
                }
            }
            player.GrabUpdate(eu);
        }

        private void HookPlayerUpdateAnimation(On.Player.orig_UpdateAnimation orig, Player player)
        {
            if (!base.IsMe(player))
            {
                orig.Invoke(player);
                return;
            }
            if (player.longBellySlide && player.animation != Player.AnimationIndex.BellySlide)
            {
                player.longBellySlide = false;
            }
            if (player.stopRollingCounter > 0 && player.animation != Player.AnimationIndex.Roll)
            {
                player.stopRollingCounter = 0;
            }
            if (player.slideUpPole > 0 && player.animation != Player.AnimationIndex.ClimbOnBeam)
            {
                player.slideUpPole = 0;
            }
            switch (player.animation)
            {
                case Player.AnimationIndex.CrawlTurn:
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        BodyChunk bodyChunk = ((Creature)player).bodyChunks[0];
                        bodyChunk.vel.x = bodyChunk.vel.x + (float)player.flipDirection;
                        BodyChunk bodyChunk2 = ((Creature)player).bodyChunks[1];
                        bodyChunk2.vel.x = bodyChunk2.vel.x - 2f * (float)player.flipDirection;
                        if (player.input[0].x > 0 != ((Creature)player).bodyChunks[0].pos.x < ((Creature)player).bodyChunks[1].pos.x)
                        {
                            BodyChunk bodyChunk3 = ((Creature)player).bodyChunks[0];
                            bodyChunk3.vel.y = bodyChunk3.vel.y - 3f;
                            if (((Creature)player).bodyChunks[0].pos.y < ((Creature)player).bodyChunks[1].pos.y + 2f)
                            {
                                player.animation = Player.AnimationIndex.None;
                                BodyChunk bodyChunk4 = ((Creature)player).bodyChunks[0];
                                bodyChunk4.vel.y = bodyChunk4.vel.y - 1f;
                            }
                        }
                        else
                        {
                            BodyChunk bodyChunk5 = ((Creature)player).bodyChunks[0];
                            bodyChunk5.vel.y = bodyChunk5.vel.y + 2f;
                        }
                        if (player.input[0].x == 0 || ((Creature)player).IsTileSolid(1, 0, 1))
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        break;
                    }
                case Player.AnimationIndex.StandUp:
                    if (player.standing)
                    {
                        BodyChunk bodyChunk6 = ((Creature)player).bodyChunks[0];
                        bodyChunk6.vel.x = bodyChunk6.vel.x * 0.7f;
                        if (!((Creature)player).IsTileSolid(0, 0, 1) && (((Creature)player).bodyChunks[1].onSlope == 0 || player.input[0].x == 0))
                        {
                            player.bodyMode = Player.BodyModeIndex.Stand;
                            if (((Creature)player).bodyChunks[0].pos.y > ((Creature)player).bodyChunks[1].pos.y + 3f)
                            {
                                player.animation = Player.AnimationIndex.None;
                                player.room.PlaySound(SoundID.Slugcat_Regain_Footing, ((Creature)player).bodyChunks[1]);
                            }
                        }
                        else
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    else
                    {
                        player.animation = Player.AnimationIndex.DownOnFours;
                    }
                    break;
                case Player.AnimationIndex.DownOnFours:
                    if (!player.standing)
                    {
                        BodyChunk bodyChunk7 = ((Creature)player).bodyChunks[0];
                        bodyChunk7.vel.y = bodyChunk7.vel.y - 2f;
                        BodyChunk bodyChunk8 = ((Creature)player).bodyChunks[0];
                        bodyChunk8.vel.x = bodyChunk8.vel.x + (float)player.flipDirection;
                        BodyChunk bodyChunk9 = ((Creature)player).bodyChunks[1];
                        bodyChunk9.vel.x = bodyChunk9.vel.x - (float)player.flipDirection;
                        if (((Creature)player).bodyChunks[0].pos.y < ((Creature)player).bodyChunks[1].pos.y || ((Creature)player).bodyChunks[0].ContactPoint.y == -1)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    else
                    {
                        player.animation = Player.AnimationIndex.StandUp;
                    }
                    break;
                case Player.AnimationIndex.LedgeCrawl:
                    {
                        BodyChunk bodyChunk10 = ((Creature)player).bodyChunks[0];
                        bodyChunk10.vel.x = bodyChunk10.vel.x + (float)player.flipDirection * 2f;
                        player.bodyMode = Player.BodyModeIndex.Crawl;
                        if (!((Creature)player).IsTileSolid(0, player.flipDirection, 0))
                        {
                            if ((((Creature)player).IsTileSolid(0, 0, -1) && ((Creature)player).IsTileSolid(1, 0, -1) && player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos).y == player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos).y) || (((Creature)player).bodyChunks[0].ContactPoint.x == player.flipDirection && player.input[0].x != 0) || (((Creature)player).bodyChunks[0].ContactPoint.y > -1 && ((Creature)player).bodyChunks[1].ContactPoint.y > -1))
                            {
                                player.animation = Player.AnimationIndex.None;
                            }
                        }
                        break;
                    }
                case Player.AnimationIndex.LedgeGrab:
                    player.bodyMode = Player.BodyModeIndex.Default;
                    if (((Creature)player).IsTileSolid(0, player.flipDirection, 0) && !((Creature)player).IsTileSolid(0, player.flipDirection, 1))
                    {
                        ((Creature)player).bodyChunks[0].vel *= 0.5f;
                        ((Creature)player).bodyChunks[0].pos = (((Creature)player).bodyChunks[0].pos + (player.room.MiddleOfTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos)) + new Vector2((float)player.flipDirection * (float)player.ledgeGrabCounter, 8f + (float)player.ledgeGrabCounter))) / 2f;
                        ((Creature)player).bodyChunks[0].lastPos = ((Creature)player).bodyChunks[0].pos;
                        BodyChunk bodyChunk11 = ((Creature)player).bodyChunks[1];
                        bodyChunk11.vel.x = bodyChunk11.vel.x + (float)player.flipDirection;
                        player.canJump = 1;
                        if (player.input[0].x == player.flipDirection || player.input[0].y == 1)
                        {
                            player.ledgeGrabCounter++;
                            ((Creature)player).bodyChunks[1].vel += new Vector2(-0.5f * (float)player.flipDirection, -0.5f);
                        }
                        else if (player.ledgeGrabCounter > 0)
                        {
                            player.ledgeGrabCounter--;
                        }
                        if (player.input[0].y == -1 && player.input[1].y != -1)
                        {
                            BodyChunk bodyChunk12 = ((Creature)player).bodyChunks[0];
                            bodyChunk12.pos.y = bodyChunk12.pos.y - 10f;
                            player.input[1].y = -1;
                            player.animation = Player.AnimationIndex.None;
                            player.ledgeGrabCounter = 0;
                        }
                        else if (player.input[0].x == -player.flipDirection && player.input[1].x == 0)
                        {
                            BodyChunk bodyChunk13 = ((Creature)player).bodyChunks[0];
                            bodyChunk13.vel.y = bodyChunk13.vel.y + 10f;
                            player.animation = Player.AnimationIndex.None;
                            player.ledgeGrabCounter = 0;
                        }
                        player.standing = true;
                    }
                    else
                    {
                        player.animation = Player.AnimationIndex.None;
                        player.ledgeGrabCounter = 0;
                    }
                    break;
                case Player.AnimationIndex.HangFromBeam:
                    {
                        player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                        player.standing = true;
                        ((Creature)player).bodyChunks[0].vel.y = 0f;
                        BodyChunk bodyChunk14 = ((Creature)player).bodyChunks[0];
                        bodyChunk14.vel.x = bodyChunk14.vel.x * 0.2f;
                        ((Creature)player).bodyChunks[0].pos.y = player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).y;
                        if (player.input[0].x != 0 && ((Creature)player).bodyChunks[0].ContactPoint.x != player.input[0].x)
                        {
                            if (((Creature)player).bodyChunks[1].ContactPoint.x != player.input[0].x)
                            {
                                BodyChunk bodyChunk15 = ((Creature)player).bodyChunks[0];
                                bodyChunk15.vel.x = bodyChunk15.vel.x + (float)player.input[0].x * Mathf.Lerp(1.2f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * RWCustom.Custom.LerpMap((float)player.slowMovementStun, 0f, 10f, 1f, 0.5f);
                            }
                            player.animationFrame++;
                            if (player.animationFrame > 20)
                            {
                                player.animationFrame = 1;
                                player.room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                player.AerobicIncrease(0.05f);
                            }
                            BodyChunk bodyChunk16 = ((Creature)player).bodyChunks[1];
                            bodyChunk16.vel.x = bodyChunk16.vel.x + (float)player.flipDirection * (0.5f + 0.5f * Mathf.Sin((float)player.animationFrame / 20f * 3.14159274f * 2f)) * -0.5f;
                        }
                        else if (player.animationFrame < 10)
                        {
                            player.animationFrame++;
                        }
                        else if (player.animationFrame > 10)
                        {
                            player.animationFrame--;
                        }
                        bool flag = false;
                        if (player.input[0].y < 0 && player.input[1].y == 0)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        else if (player.input[0].y > 0 && player.input[1].y == 0)
                        {
                            if (player.room.GetTile(((Creature)player).bodyChunks[0].pos).verticalBeam)
                            {
                                player.animation = Player.AnimationIndex.ClimbOnBeam;
                                if (((Creature)player).bodyChunks[0].pos.x < player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).x)
                                {
                                    player.flipDirection = -1;
                                }
                                else
                                {
                                    player.flipDirection = 1;
                                }
                            }
                            else
                            {
                                flag = true;
                            }
                        }
                        else if (player.input[0].jmp && !player.input[1].jmp)
                        {
                            flag = true;
                        }
                        if (flag)
                        {
                            player.room.PlaySound(SoundID.Slugcat_Get_Up_On_Horizontal_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                            player.animation = Player.AnimationIndex.GetUpOnBeam;
                            player.straightUpOnHorizontalBeam = false;
                            if (player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)player.flipDirection * 20f, 0f)).Terrain == Room.Tile.TerrainType.Solid || !player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)player.flipDirection * 20f, 0f)).horizontalBeam)
                            {
                                player.flipDirection = -player.flipDirection;
                            }
                            if (player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)player.flipDirection * 20f, 0f)).Terrain == Room.Tile.TerrainType.Solid || !player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)player.flipDirection * 20f, 0f)).horizontalBeam)
                            {
                                player.flipDirection = -player.flipDirection;
                                player.straightUpOnHorizontalBeam = true;
                            }
                            if (!player.straightUpOnHorizontalBeam && player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2((float)player.flipDirection * 20f, 20f)).Solid)
                            {
                                player.straightUpOnHorizontalBeam = true;
                            }
                            player.upOnHorizontalBeamPos = new Vector2(((Creature)player).bodyChunks[0].pos.x, player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).y + 20f);
                        }
                        if (!player.room.GetTile(((Creature)player).bodyChunks[0].pos).horizontalBeam)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        break;
                    }
                case Player.AnimationIndex.GetUpOnBeam:
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    ((Creature)player).bodyChunks[0].vel.x = 0f;
                    ((Creature)player).bodyChunks[0].vel.y = 0f;
                    player.forceFeetToHorizontalBeamTile = 20;
                    if (player.straightUpOnHorizontalBeam)
                    {
                        if (player.input[0].y < 0 || ((Creature)player).mainBodyChunk.ContactPoint.y > 0)
                        {
                            player.straightUpOnHorizontalBeam = false;
                        }
                        if (player.room.GetTile(player.upOnHorizontalBeamPos).Solid)
                        {
                            for (int i = 1; i >= -1; i -= 2)
                            {
                                if (!player.room.GetTile(player.upOnHorizontalBeamPos + new Vector2((float)(player.flipDirection * i) * 20f, 0f)).Solid)
                                {
                                    player.upOnHorizontalBeamPos.x = player.upOnHorizontalBeamPos.x + (float)(player.flipDirection * i) * 20f;
                                    break;
                                }
                            }
                        }
                        ((Creature)player).mainBodyChunk.vel += RWCustom.Custom.DirVec(((Creature)player).mainBodyChunk.pos, player.upOnHorizontalBeamPos) * 1.8f;
                        ((Creature)player).bodyChunks[1].vel += RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, player.upOnHorizontalBeamPos + new Vector2(0f, -20f)) * 1.8f;
                        if (player.room.GetTile(((Creature)player).bodyChunks[1].pos).horizontalBeam && ((Creature)player).bodyChunks[1].pos.y > player.upOnHorizontalBeamPos.y - 25f)
                        {
                            player.noGrabCounter = 15;
                            player.animation = Player.AnimationIndex.StandOnBeam;
                            ((Creature)player).bodyChunks[1].pos.y = player.room.MiddleOfTile(((Creature)player).bodyChunks[1].pos).y + 5f;
                            ((Creature)player).bodyChunks[1].vel.y = 0f;
                        }
                        else if ((!player.room.GetTile(((Creature)player).bodyChunks[0].pos).horizontalBeam && !player.room.GetTile(((Creature)player).bodyChunks[1].pos).horizontalBeam) || !RWCustom.Custom.DistLess(((Creature)player).mainBodyChunk.pos, player.upOnHorizontalBeamPos, 25f))
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    else
                    {
                        ((Creature)player).bodyChunks[0].pos.y = player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).y;
                        BodyChunk bodyChunk17 = ((Creature)player).bodyChunks[1];
                        bodyChunk17.vel.y = bodyChunk17.vel.y + 2f;
                        BodyChunk bodyChunk18 = ((Creature)player).bodyChunks[1];
                        bodyChunk18.vel.x = bodyChunk18.vel.x + (float)player.flipDirection * 0.5f;
                        if (((Creature)player).bodyChunks[1].pos.y > ((Creature)player).mainBodyChunk.pos.y - 15f && !player.room.GetTile(((Creature)player).mainBodyChunk.pos + new Vector2(Mathf.Sign(((Creature)player).bodyChunks[1].pos.x - ((Creature)player).mainBodyChunk.pos.x) * 35f, 0f)).horizontalBeam && player.room.GetTile(((Creature)player).mainBodyChunk.pos + new Vector2(Mathf.Sign(((Creature)player).bodyChunks[1].pos.x - ((Creature)player).mainBodyChunk.pos.x) * -15f, 0f)).horizontalBeam)
                        {
                            BodyChunk mainBodyChunk = ((Creature)player).mainBodyChunk;
                            mainBodyChunk.vel.x = mainBodyChunk.vel.x - Mathf.Sign(((Creature)player).bodyChunks[1].pos.x - ((Creature)player).mainBodyChunk.pos.x) * 1.5f;
                            BodyChunk bodyChunk19 = ((Creature)player).bodyChunks[1];
                            bodyChunk19.vel.x = bodyChunk19.vel.x - Mathf.Sign(((Creature)player).bodyChunks[1].pos.x - ((Creature)player).mainBodyChunk.pos.x) * 0.5f;
                        }
                        if (((Creature)player).bodyChunks[1].ContactPoint.y > 0)
                        {
                            if (!player.room.GetTile(((Creature)player).mainBodyChunk.pos + new Vector2(0f, 20f)).Solid)
                            {
                                player.straightUpOnHorizontalBeam = true;
                            }
                            else
                            {
                                player.animation = Player.AnimationIndex.HangFromBeam;
                            }
                        }
                        if (((Creature)player).bodyChunks[1].pos.y > ((Creature)player).bodyChunks[0].pos.y)
                        {
                            player.noGrabCounter = 15;
                            player.animation = Player.AnimationIndex.StandOnBeam;
                            ((Creature)player).bodyChunks[1].pos.y = player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).y + 5f;
                            ((Creature)player).bodyChunks[1].vel.y = 0f;
                        }
                        if (!player.room.GetTile(((Creature)player).bodyChunks[0].pos).horizontalBeam)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    break;
                case Player.AnimationIndex.StandOnBeam:
                    {
                        player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                        player.standing = true;
                        player.canJump = 5;
                        BodyChunk bodyChunk20 = ((Creature)player).bodyChunks[1];
                        bodyChunk20.vel.x = bodyChunk20.vel.x * 0.5f;
                        if (((Creature)player).bodyChunks[0].ContactPoint.y < 1 || !((Creature)player).IsTileSolid(1, 0, 1))
                        {
                            ((Creature)player).bodyChunks[1].vel.y = 0f;
                            ((Creature)player).bodyChunks[1].pos.y = player.room.MiddleOfTile(((Creature)player).bodyChunks[1].pos).y + 5f;
                            BodyChunk bodyChunk21 = ((Creature)player).bodyChunks[0];
                            bodyChunk21.vel.y = bodyChunk21.vel.y + 2f;
                            player.dynamicRunSpeed[0] = 2.1f * player.slugcatStats.runspeedFac;
                            player.dynamicRunSpeed[1] = 2.1f * player.slugcatStats.runspeedFac;
                            if (player.input[0].y < 0 && player.input[1].y == 0)
                            {
                                player.animation = Player.AnimationIndex.None;
                            }
                        }
                        else
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        if (player.input[0].x != 0)
                        {
                            player.animationFrame++;
                        }
                        else
                        {
                            player.animationFrame = 0;
                        }
                        if (player.animationFrame > 6)
                        {
                            player.animationFrame = 0;
                            player.room.PlaySound(SoundID.Slugcat_Walk_On_Horizontal_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                        }
                        if (player.input[0].y == 1 && player.input[1].y == 0 && player.room.GetTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos) + new RWCustom.IntVector2(0, 1)).horizontalBeam)
                        {
                            BodyChunk bodyChunk22 = ((Creature)player).bodyChunks[0];
                            bodyChunk22.pos.y = bodyChunk22.pos.y + 8f;
                            BodyChunk bodyChunk23 = ((Creature)player).bodyChunks[1];
                            bodyChunk23.pos.y = bodyChunk23.pos.y + 8f;
                            player.animation = Player.AnimationIndex.HangFromBeam;
                        }
                        break;
                    }
                case Player.AnimationIndex.ClimbOnBeam:
                    {
                        player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                        player.standing = true;
                        player.canJump = 1;
                        for (int j = 0; j < 2; j++)
                        {
                            if (((Creature)player).bodyChunks[j].ContactPoint.x != 0)
                            {
                                player.flipDirection = -((Creature)player).bodyChunks[j].ContactPoint.x;
                            }
                        }
                    ((Creature)player).bodyChunks[0].vel.x = 0f;
                        bool flag2 = true;
                        if (!((Creature)player).IsTileSolid(0, 0, 1) && player.input[0].y > 0 && (((Creature)player).bodyChunks[0].ContactPoint.y < 0 || ((Creature)player).IsTileSolid(0, player.flipDirection, 1)))
                        {
                            flag2 = false;
                        }
                        if (flag2 && ((Creature)player).IsTileSolid(0, player.flipDirection, 0))
                        {
                            player.flipDirection = -player.flipDirection;
                        }
                        if (flag2)
                        {
                            ((Creature)player).bodyChunks[0].pos.x = (((Creature)player).bodyChunks[0].pos.x + player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).x + (float)player.flipDirection * 5f) / 2f;
                            ((Creature)player).bodyChunks[1].pos.x = (((Creature)player).bodyChunks[1].pos.x * 7f + player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).x + (float)player.flipDirection * 5f) / 8f;
                        }
                        else
                        {
                            ((Creature)player).bodyChunks[0].pos.x = (((Creature)player).bodyChunks[0].pos.x + player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).x) / 2f;
                            ((Creature)player).bodyChunks[1].pos.x = (((Creature)player).bodyChunks[1].pos.x * 7f + player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).x) / 8f;
                        }
                        BodyChunk bodyChunk24 = ((Creature)player).bodyChunks[0];
                        bodyChunk24.vel.y = bodyChunk24.vel.y * 0.5f;
                        if (player.input[0].y > 0)
                        {
                            player.animationFrame++;
                            if (player.animationFrame > 20)
                            {
                                player.animationFrame = 0;
                                player.room.PlaySound(SoundID.Slugcat_Climb_Up_Vertical_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                player.AerobicIncrease(0.1f);
                            }
                            BodyChunk bodyChunk25 = ((Creature)player).bodyChunks[0];
                            bodyChunk25.vel.y = bodyChunk25.vel.y + Mathf.Lerp(1f, 1.4f, player.Adrenaline) * player.slugcatStats.poleClimbSpeedFac * RWCustom.Custom.LerpMap((float)player.slowMovementStun, 0f, 10f, 1f, 0.2f);
                        }
                        else if (player.input[0].y < 0)
                        {
                            BodyChunk bodyChunk26 = ((Creature)player).bodyChunks[0];
                            bodyChunk26.vel.y = bodyChunk26.vel.y - 2.2f * (0.2f + 0.8f * player.room.gravity);
                        }
                        BodyChunk bodyChunk27 = ((Creature)player).bodyChunks[0];
                        bodyChunk27.vel.y = bodyChunk27.vel.y + (1f + ((Creature)player).gravity);
                        BodyChunk bodyChunk28 = ((Creature)player).bodyChunks[1];
                        bodyChunk28.vel.y = bodyChunk28.vel.y - (1f - ((Creature)player).gravity);
                        if (player.slideUpPole > 0)
                        {
                            player.slideUpPole--;
                            if (player.slideUpPole > 8)
                            {
                                player.animationFrame = 12;
                            }
                            if (player.slideUpPole == 0)
                            {
                                player.slowMovementStun = Math.Max(player.slowMovementStun, 16);
                            }
                            if (player.slideUpPole > 14)
                            {
                                BodyChunk bodyChunk29 = ((Creature)player).bodyChunks[0];
                                bodyChunk29.pos.y = bodyChunk29.pos.y + 2f;
                                BodyChunk bodyChunk30 = ((Creature)player).bodyChunks[1];
                                bodyChunk30.pos.y = bodyChunk30.pos.y + 2f;
                            }
                            BodyChunk bodyChunk31 = ((Creature)player).bodyChunks[0];
                            bodyChunk31.vel.y = bodyChunk31.vel.y + RWCustom.Custom.LerpMap((float)player.slideUpPole, 17f, 0f, 3f, -1.2f, 0.45f);
                            BodyChunk bodyChunk32 = ((Creature)player).bodyChunks[1];
                            bodyChunk32.vel.y = bodyChunk32.vel.y + RWCustom.Custom.LerpMap((float)player.slideUpPole, 17f, 0f, 1.5f, -1.4f, 0.45f);
                        }
                    ((Creature)player).GoThroughFloors = (player.input[0].x == 0 && player.input[0].downDiagonal == 0);
                        if (player.input[0].x != 0 && player.input[1].x != player.input[0].x && player.input[0].x == player.flipDirection && player.input[0].x == player.lastFlipDirection)
                        {
                            if (player.room.GetTile(((Creature)player).bodyChunks[0].pos).horizontalBeam && !((Creature)player).IsTileSolid(0, 0, -1))
                            {
                                player.animation = Player.AnimationIndex.HangFromBeam;
                            }
                            else if (player.room.GetTile(((Creature)player).bodyChunks[1].pos).horizontalBeam)
                            {
                                player.animation = Player.AnimationIndex.StandOnBeam;
                            }
                        }
                        if (player.input[0].x == player.flipDirection && player.input[1].x == 0 && player.flipDirection == player.lastFlipDirection && player.room.GetTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos) + new RWCustom.IntVector2(player.flipDirection, 0)).verticalBeam)
                        {
                            ((Creature)player).bodyChunks[0].pos.x = player.room.MiddleOfTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos) + new RWCustom.IntVector2(player.flipDirection, 0)).x - (float)player.flipDirection * 5f;
                            player.flipDirection = -player.flipDirection;
                            player.jumpStun = 11 * player.flipDirection;
                        }
                        if (((Creature)player).bodyChunks[1].ContactPoint.y < 0 && player.input[0].y < 0)
                        {
                            player.room.PlaySound(SoundID.Slugcat_Regain_Footing, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                            player.animation = Player.AnimationIndex.StandUp;
                        }
                        if (!player.room.GetTile(((Creature)player).bodyChunks[0].pos).verticalBeam)
                        {
                            player.animation = Player.AnimationIndex.None;
                            if (player.room.GetTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos) + new RWCustom.IntVector2(0, -1)).verticalBeam)
                            {
                                player.room.PlaySound(SoundID.Slugcat_Get_Up_On_Top_Of_Vertical_Beam_Tip, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                player.animation = Player.AnimationIndex.GetUpToBeamTip;
                            }
                            else if (player.room.GetTile(player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos) + new RWCustom.IntVector2(0, 1)).verticalBeam)
                            {
                                player.animation = Player.AnimationIndex.HangUnderVerticalBeam;
                            }
                        }
                        break;
                    }
                case Player.AnimationIndex.GetUpToBeamTip:
                    {
                        player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                        player.standing = true;
                        player.canJump = 5;
                        BodyChunk bodyChunk33 = ((Creature)player).bodyChunks[0];
                        bodyChunk33.vel.y = bodyChunk33.vel.y + ((Creature)player).gravity;
                        BodyChunk bodyChunk34 = ((Creature)player).bodyChunks[1];
                        bodyChunk34.vel.y = bodyChunk34.vel.y + ((Creature)player).gravity;
                        Vector2 p = new Vector2(0f, 0f);
                        for (int k = 0; k < 2; k++)
                        {
                            if (!player.room.GetTile(((Creature)player).bodyChunks[k].pos).verticalBeam && player.room.GetTile(((Creature)player).bodyChunks[k].pos + new Vector2(0f, -20f)).verticalBeam)
                            {
                                p = player.room.MiddleOfTile(((Creature)player).bodyChunks[k].pos);
                                break;
                            }
                        }
                        if (p.x != 0f || p.y != 0f)
                        {
                            ((Creature)player).bodyChunks[0].pos.x = (((Creature)player).bodyChunks[0].pos.x * 14f + p.x) / 15f;
                            ((Creature)player).bodyChunks[1].pos.x = (((Creature)player).bodyChunks[1].pos.x * 4f + p.x) / 5f;
                            BodyChunk bodyChunk35 = ((Creature)player).bodyChunks[0];
                            bodyChunk35.vel.y = bodyChunk35.vel.y + 0.1f;
                            ((Creature)player).bodyChunks[1].pos.y = (((Creature)player).bodyChunks[1].pos.y * 4f + p.y) / 5f;
                            if (RWCustom.Custom.DistLess(((Creature)player).bodyChunks[1].pos, p, 6f))
                            {
                                player.animation = Player.AnimationIndex.BeamTip;
                                player.room.PlaySound(SoundID.Slugcat_Regain_Footing, ((Creature)player).mainBodyChunk, false, 0.3f, 1f);
                            }
                        }
                        else
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        break;
                    }
                case Player.AnimationIndex.HangUnderVerticalBeam:
                    player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                    player.standing = false;
                    if ((player.input[0].y < 0 && player.input[1].y == 0) || ((Creature)player).bodyChunks[1].vel.magnitude > 10f || ((Creature)player).bodyChunks[0].vel.magnitude > 10f || !player.room.GetTile(((Creature)player).bodyChunks[0].pos + new Vector2(0f, 20f)).verticalBeam)
                    {
                        player.animation = Player.AnimationIndex.None;
                        player.standing = true;
                    }
                    else
                    {
                        ((Creature)player).bodyChunks[0].pos.x = Mathf.Lerp(((Creature)player).bodyChunks[0].pos.x, player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).x, 0.5f);
                        ((Creature)player).bodyChunks[0].pos.y = Mathf.Max(((Creature)player).bodyChunks[0].pos.y, player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos).y + 5f + ((Creature)player).bodyChunks[0].vel.y);
                        BodyChunk bodyChunk36 = ((Creature)player).bodyChunks[0];
                        bodyChunk36.vel.x = bodyChunk36.vel.x * 0f;
                        BodyChunk bodyChunk37 = ((Creature)player).bodyChunks[0];
                        bodyChunk37.vel.y = bodyChunk37.vel.y * 0.5f;
                        BodyChunk bodyChunk38 = ((Creature)player).bodyChunks[1];
                        bodyChunk38.vel.x = bodyChunk38.vel.x + (float)player.input[0].x;
                        if (player.input[0].y > 0)
                        {
                            BodyChunk bodyChunk39 = ((Creature)player).bodyChunks[0];
                            bodyChunk39.vel.y = bodyChunk39.vel.y + 2.5f;
                        }
                        if (player.room.GetTile(((Creature)player).bodyChunks[0].pos).verticalBeam)
                        {
                            player.animation = Player.AnimationIndex.ClimbOnBeam;
                        }
                    }
                    if (player.input[0].jmp && !player.input[1].jmp)
                    {
                        player.animation = Player.AnimationIndex.None;
                        if (player.input[0].x == 0)
                        {
                            BodyChunk bodyChunk40 = ((Creature)player).bodyChunks[0];
                            bodyChunk40.pos.y = bodyChunk40.pos.y + 16f;
                            ((Creature)player).bodyChunks[0].vel.y = 10f;
                            player.standing = true;
                        }
                        else
                        {
                            BodyChunk bodyChunk41 = ((Creature)player).bodyChunks[1];
                            bodyChunk41.vel.y = bodyChunk41.vel.y + 4f;
                            BodyChunk bodyChunk42 = ((Creature)player).bodyChunks[1];
                            bodyChunk42.vel.x = bodyChunk42.vel.x + 2f * (float)player.input[0].x;
                            BodyChunk bodyChunk43 = ((Creature)player).bodyChunks[0];
                            bodyChunk43.vel.y = bodyChunk43.vel.y + 6f;
                            BodyChunk bodyChunk44 = ((Creature)player).bodyChunks[0];
                            bodyChunk44.vel.x = bodyChunk44.vel.x + 3f * (float)player.input[0].x;
                        }
                    }
                    break;
                case Player.AnimationIndex.BeamTip:
                    {
                        player.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
                        player.standing = true;
                        player.canJump = 5;
                        ((Creature)player).bodyChunks[1].vel *= 0.5f;
                        ((Creature)player).bodyChunks[1].pos = (((Creature)player).bodyChunks[1].pos + player.room.MiddleOfTile(((Creature)player).bodyChunks[1].pos)) / 2f;
                        BodyChunk bodyChunk45 = ((Creature)player).bodyChunks[0];
                        bodyChunk45.vel.y = bodyChunk45.vel.y + 1.5f;
                        BodyChunk bodyChunk46 = ((Creature)player).bodyChunks[0];
                        bodyChunk46.vel.y = bodyChunk46.vel.y + (float)player.input[0].y * 0.1f;
                        BodyChunk bodyChunk47 = ((Creature)player).bodyChunks[0];
                        bodyChunk47.vel.x = bodyChunk47.vel.x + (float)player.input[0].x * 0.1f;
                        if (player.input[0].y > 0 && player.input[1].y == 0)
                        {
                            BodyChunk bodyChunk48 = ((Creature)player).bodyChunks[1];
                            bodyChunk48.vel.y = bodyChunk48.vel.y - 1f;
                            player.canJump = 0;
                            player.animation = Player.AnimationIndex.None;
                        }
                        if ((player.input[0].y < 0 && player.input[1].y == 0) || ((Creature)player).bodyChunks[0].pos.y < ((Creature)player).bodyChunks[1].pos.y || !player.room.GetTile(((Creature)player).bodyChunks[1].pos + new Vector2(0f, -20f)).verticalBeam)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        break;
                    }
                case Player.AnimationIndex.CorridorTurn:
                    if (player.corridorTurnDir != null && player.bodyMode == Player.BodyModeIndex.CorridorClimb && player.corridorTurnCounter < 40)
                    {
                        player.slowMovementStun = Math.Max(10, player.slowMovementStun);
                        ((Creature)player).mainBodyChunk.vel *= 0.5f;
                        ((Creature)player).bodyChunks[1].vel *= 0.5f;
                        if (player.corridorTurnCounter < 30)
                        {
                            ((Creature)player).mainBodyChunk.vel += RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f);
                        }
                        else
                        {
                            ((Creature)player).mainBodyChunk.vel += player.corridorTurnDir.Value.ToVector2() * 0.5f;
                            ((Creature)player).bodyChunks[1].vel -= player.corridorTurnDir.Value.ToVector2() * 0.5f;
                        }
                        player.corridorTurnCounter++;
                    }
                    else
                    {
                        ((Creature)player).mainBodyChunk.vel += player.corridorTurnDir.Value.ToVector2() * 6f;
                        ((Creature)player).bodyChunks[1].vel += player.corridorTurnDir.Value.ToVector2() * 5f;
                        if (((Creature)player).graphicsModule != null)
                        {
                            for (int l = 0; l < ((Creature)player).graphicsModule.bodyParts.Length; l++)
                            {
                                ((Creature)player).graphicsModule.bodyParts[l].vel -= player.corridorTurnDir.Value.ToVector2() * 10f;
                            }
                        }
                        player.corridorTurnDir = null;
                        player.animation = Player.AnimationIndex.None;
                        player.room.PlaySound(SoundID.Slugcat_Turn_In_Corridor, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                    }
                    break;
                case Player.AnimationIndex.SurfaceSwim:
                    if (((Creature)player).grasps[0] != null && ((Creature)player).grasps[0].grabbed is JetFish && (((Creature)player).grasps[0].grabbed as JetFish).Consious)
                    {
                        player.dynamicRunSpeed[0] = 0f;
                        player.dynamicRunSpeed[1] = 0f;
                        ((Creature)player).waterFriction = 1f;
                    }
                    else
                    {
                        player.canJump = 0;
                        player.swimCycle += 0.025f;
                        ((Creature)player).waterFriction = 0.96f;
                        player.swimForce *= 0.5f;
                        if (player.input[0].y > -1 && ((Creature)player).bodyChunks[0].vel.y > -5f && ((Creature)player).bodyChunks[0].vel.y < 3f && player.waterJumpDelay == 0 && !player.input[0].jmp)
                        {
                            BodyChunk bodyChunk49 = ((Creature)player).bodyChunks[0];
                            bodyChunk49.vel.y = bodyChunk49.vel.y * 0.8f;
                            BodyChunk bodyChunk50 = ((Creature)player).bodyChunks[1];
                            bodyChunk50.vel.y = bodyChunk50.vel.y * 0.8f;
                            BodyChunk bodyChunk51 = ((Creature)player).bodyChunks[0];
                            bodyChunk51.vel.y = bodyChunk51.vel.y + Mathf.Clamp((((Creature)player).bodyChunks[0].pos.y - (player.room.FloatWaterLevel(((Creature)player).bodyChunks[0].pos.x) + 15f)) * -0.1f, -0.5f, 1.5f);
                            BodyChunk bodyChunk52 = ((Creature)player).bodyChunks[1];
                            bodyChunk52.vel.y = bodyChunk52.vel.y - 0.5f;
                        }
                        else if (player.input[0].y == -1)
                        {
                            BodyChunk bodyChunk53 = ((Creature)player).bodyChunks[0];
                            bodyChunk53.vel.y = bodyChunk53.vel.y - 0.2f;
                            BodyChunk bodyChunk54 = ((Creature)player).bodyChunks[1];
                            bodyChunk54.vel.y = bodyChunk54.vel.y + 0.1f;
                        }
                        else if (player.input[0].y == 1)
                        {
                            BodyChunk bodyChunk55 = ((Creature)player).bodyChunks[0];
                            bodyChunk55.vel.y = bodyChunk55.vel.y + 0.5f;
                        }
                        player.dynamicRunSpeed[0] = 5f; // # 2.7f -> 5f
                        player.dynamicRunSpeed[1] = 0f;
                        if (player.input[0].x != 0)
                        {
                            BodyChunk bodyChunk56 = ((Creature)player).bodyChunks[1];
                            bodyChunk56.vel.x = bodyChunk56.vel.x - (float)player.input[0].x * Mathf.Lerp(0.3f, 0.45f, player.Adrenaline); // # 0.2f, 0.3f -> 0.3f, 0.45f
                            player.swimCycle += 0.0333333351f;
                        }
                        if (player.input[0].jmp && !player.input[1].jmp)
                        {
                            if (player.waterJumpDelay == 0)
                            {
                                if (((Creature)player).bodyChunks[0].vel.y < 2f && ((Creature)player).bodyChunks[1].vel.y < 2f)
                                {
                                    BodyChunk bodyChunk57 = ((Creature)player).bodyChunks[0];
                                    bodyChunk57.vel.y = bodyChunk57.vel.y + 12f; // # 6f -> 12f
                                    BodyChunk bodyChunk58 = ((Creature)player).bodyChunks[1];
                                    bodyChunk58.vel.y = bodyChunk58.vel.y + 12f; // # 6f -> 12f
                                }
                                else
                                {
                                    ((Creature)player).bodyChunks[0].vel += RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos) * 8f; // # 4f -> 8f
                                    BodyChunk bodyChunk59 = ((Creature)player).bodyChunks[1];
                                    bodyChunk59.vel.y = bodyChunk59.vel.y + ((((Creature)player).bodyChunks[1].vel.y >= 4f) ? 1.5f : 3f) * 2f; // # a + b -> a + b * 2f
                                }
                                player.waterJumpDelay = 17;
                            }
                            else
                            {
                                if (player.waterJumpDelay < 10)
                                {
                                    player.waterJumpDelay = 10;
                                }
                                ((Creature)player).bodyChunks[0].vel += new Vector2(0f, 8f); // # 0f, 4f -> 0f, 8f
                                BodyChunk bodyChunk60 = ((Creature)player).bodyChunks[1];
                                bodyChunk60.vel.x = bodyChunk60.vel.x * 0.75f;
                            }
                        }
                        if (player.bodyMode != Player.BodyModeIndex.Swimming)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    break;
                case Player.AnimationIndex.DeepSwim:
                    player.dynamicRunSpeed[0] = 0f;
                    player.dynamicRunSpeed[1] = 0f;
                    if (((Creature)player).grasps[0] != null && ((Creature)player).grasps[0].grabbed is JetFish && (((Creature)player).grasps[0].grabbed as JetFish).Consious)
                    {
                        ((Creature)player).waterFriction = 1f;
                    }
                    else
                    {
                        player.canJump = 0;
                        player.standing = false;
                        ((Creature)player).GoThroughFloors = true;
                        float num = (Mathf.Abs(Vector2.Dot(((Creature)player).bodyChunks[0].vel.normalized, (((Creature)player).bodyChunks[0].pos - ((Creature)player).bodyChunks[1].pos).normalized)) + Mathf.Abs(Vector2.Dot(((Creature)player).bodyChunks[1].vel.normalized, (((Creature)player).bodyChunks[0].pos - ((Creature)player).bodyChunks[1].pos).normalized))) / 2f;
                        if (player.input[0].jmp && !player.input[1].jmp && player.airInLungs > 0.5f)
                        {
                            if (player.waterJumpDelay == 0)
                            {
                                player.swimCycle = 2.7f;
                                ((Creature)player).bodyChunks[0].vel += RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos) * 3f;
                                player.airInLungs -= 0.2f;
                                if (player.room.BeingViewed)
                                {
                                    player.room.AddObject(new Bubble(((Creature)player).mainBodyChunk.pos, ((Creature)player).mainBodyChunk.vel, false, false));
                                }
                            }
                            else
                            {
                                player.swimCycle = 0f;
                            }
                            player.waterJumpDelay = 20;
                        }
                        player.swimCycle += 0.01f;
                        if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                        {
                            float value = Vector2.Angle(((Creature)player).bodyChunks[0].lastPos - ((Creature)player).bodyChunks[1].lastPos, ((Creature)player).bodyChunks[0].pos - ((Creature)player).bodyChunks[1].pos);
                            float num2 = 0.2f + Mathf.InverseLerp(0f, 12f, value) * 0.8f;
                            if (player.slowMovementStun > 0)
                            {
                                num2 *= 0.5f;
                            }
                            num2 *= Mathf.Lerp(1f, 1.2f, player.Adrenaline);
                            if (num2 > player.swimForce)
                            {
                                player.swimForce = Mathf.Lerp(player.swimForce, num2, 0.7f);
                            }
                            else
                            {
                                player.swimForce = Mathf.Lerp(player.swimForce, num2, 0.05f);
                            }
                            player.swimCycle += Mathf.Lerp(player.swimForce, 1f, 0.5f) / 10f;
                            if (player.airInLungs < 0.5f && player.airInLungs > 0.166666672f)
                            {
                                player.swimCycle += 0.05f;
                            }
                            if (((Creature)player).bodyChunks[0].ContactPoint.x != 0 || ((Creature)player).bodyChunks[0].ContactPoint.y != 0)
                            {
                                player.swimForce *= 0.5f;
                            }
                            if (player.swimCycle > 4f)
                            {
                                player.swimCycle = 0f;
                            }
                            else if (player.swimCycle > 3f)
                            {
                                ((Creature)player).bodyChunks[0].vel += RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos) * 0.7f * Mathf.Lerp(player.swimForce, 1f, 0.5f) * ((Creature)player).bodyChunks[0].submersion;
                            }
                            Vector2 vector = player.SwimDir(true);
                            if (player.airInLungs < 0.3f)
                            {
                                vector = Vector3.Slerp(vector, new Vector2(0f, 1f), Mathf.InverseLerp(0.3f, 0f, player.airInLungs));
                            }
                            ((Creature)player).bodyChunks[0].vel += vector * 0.5f * player.swimForce * Mathf.Lerp(num, 1f, 0.5f) * ((Creature)player).bodyChunks[0].submersion;
                            ((Creature)player).bodyChunks[1].vel -= vector * 0.1f * ((Creature)player).bodyChunks[0].submersion;
                            ((Creature)player).bodyChunks[0].vel += RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos) * 0.4f * player.swimForce * num * ((Creature)player).bodyChunks[0].submersion;
                            if (((Creature)player).bodyChunks[0].vel.magnitude < 3f)
                            {
                                ((Creature)player).bodyChunks[0].vel += vector * 0.2f * Mathf.InverseLerp(3f, 1.5f, ((Creature)player).bodyChunks[0].vel.magnitude);
                                ((Creature)player).bodyChunks[1].vel -= vector * 0.1f * Mathf.InverseLerp(3f, 1.5f, ((Creature)player).bodyChunks[0].vel.magnitude);
                            }
                        }
                        ((Creature)player).waterFriction = Mathf.Lerp(0.92f, 0.96f, num);
                        if (player.bodyMode != Player.BodyModeIndex.Swimming)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                    }
                    break;
                case Player.AnimationIndex.Roll:
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        Vector2 a = RWCustom.Custom.PerpendicularVector(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos);
                        ((Creature)player).bodyChunks[0].vel *= 0.9f;
                        ((Creature)player).bodyChunks[1].vel *= 0.9f;
                        ((Creature)player).bodyChunks[0].vel += a * 2f * (float)player.rollDirection;
                        ((Creature)player).bodyChunks[1].vel -= a * 2f * (float)player.rollDirection;
                        player.AerobicIncrease(0.01f);
                        bool flag3 = ((Creature)player).bodyChunks[0].ContactPoint.x == player.rollDirection || ((Creature)player).bodyChunks[1].ContactPoint.x == player.rollDirection;
                        if (((Creature)player).bodyChunks[1].onSlope == -player.rollDirection || ((Creature)player).bodyChunks[0].onSlope == -player.rollDirection)
                        {
                            ((Creature)player).bodyChunks[0].pos += a * (float)player.rollDirection;
                            ((Creature)player).bodyChunks[1].pos -= a * (float)player.rollDirection;
                        }
                        if (!((Creature)player).IsTileSolid(0, 0, -1) && !((Creature)player).IsTileSolid(1, 0, -1) && ((Creature)player).bodyChunks[0].ContactPoint.y >= 0 && ((Creature)player).bodyChunks[1].ContactPoint.y >= 0)
                        {
                            if (((Creature)player).IsTileSolid(0, 0, -2) || ((Creature)player).IsTileSolid(1, 0, -2))
                            {
                                ((Creature)player).bodyChunks[0].vel *= 0.7f;
                                ((Creature)player).bodyChunks[1].vel *= 0.7f;
                                BodyChunk bodyChunk61 = ((Creature)player).bodyChunks[0];
                                bodyChunk61.pos.y = bodyChunk61.pos.y - 2.5f;
                                BodyChunk bodyChunk62 = ((Creature)player).bodyChunks[1];
                                bodyChunk62.pos.y = bodyChunk62.pos.y - 2.5f;
                            }
                            else
                            {
                                flag3 = true;
                            }
                        }
                        else
                        {
                            BodyChunk bodyChunk63 = ((Creature)player).bodyChunks[0];
                            bodyChunk63.vel.x = bodyChunk63.vel.x + 1.1f * (float)player.rollDirection;
                            BodyChunk bodyChunk64 = ((Creature)player).bodyChunks[1];
                            bodyChunk64.vel.x = bodyChunk64.vel.x + 1.1f * (float)player.rollDirection;
                            player.canJump = Math.Max(player.canJump, 5);
                            for (int m = 0; m < 2; m++)
                            {
                                if (((Creature)player).IsTileSolid(m, player.rollDirection, 0) && !((Creature)player).IsTileSolid(m, player.rollDirection, 1) && !((Creature)player).IsTileSolid(0, 0, 1) && !((Creature)player).IsTileSolid(1, 0, 1))
                                {
                                    Debug.Log("roll up ledge");
                                    ((Creature)player).bodyChunks[0].vel *= 0.7f;
                                    ((Creature)player).bodyChunks[1].vel *= 0.7f;
                                    BodyChunk bodyChunk65 = ((Creature)player).bodyChunks[0];
                                    bodyChunk65.pos.y = bodyChunk65.pos.y + 5f;
                                    BodyChunk bodyChunk66 = ((Creature)player).bodyChunks[1];
                                    bodyChunk66.pos.y = bodyChunk66.pos.y + 5f;
                                    BodyChunk bodyChunk67 = ((Creature)player).bodyChunks[0];
                                    bodyChunk67.vel.y = bodyChunk67.vel.y + ((Creature)player).gravity;
                                    BodyChunk bodyChunk68 = ((Creature)player).bodyChunks[1];
                                    bodyChunk68.vel.y = bodyChunk68.vel.y + ((Creature)player).gravity;
                                    flag3 = false;
                                    break;
                                }
                            }
                        }
                        if (flag3)
                        {
                            player.stopRollingCounter++;
                        }
                        else
                        {
                            player.stopRollingCounter = 0;
                        }
                        if ((((player.rollCounter > 15 && player.input[0].y > -1 && player.input[0].downDiagonal == 0) || (float)player.rollCounter > 30f + 80f * player.Adrenaline || player.input[0].x == -player.rollDirection) && ((Creature)player).bodyChunks[0].pos.y > ((Creature)player).bodyChunks[1].pos.y) || (float)player.rollCounter > 60f + 80f * player.Adrenaline || player.stopRollingCounter > 6)
                        {
                            player.rollDirection = 0;
                            player.room.PlaySound(SoundID.Slugcat_Roll_Finish, ((Creature)player).mainBodyChunk.pos, 1f, 1f);
                            player.animation = Player.AnimationIndex.None;
                            player.standing = (player.input[0].y > -1);
                        }
                        break;
                    }
                case Player.AnimationIndex.Flip:
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        Vector2 a = RWCustom.Custom.PerpendicularVector(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos);
                        ((Creature)player).bodyChunks[0].vel -= a * (float)player.slideDirection * Mathf.Lerp(0.38f, 0.8f, player.Adrenaline) * ((!player.flipFromSlide) ? 1f : 2.5f);
                        ((Creature)player).bodyChunks[1].vel += a * (float)player.slideDirection * Mathf.Lerp(0.38f, 0.8f, player.Adrenaline) * ((!player.flipFromSlide) ? 1f : 2.5f);
                        player.standing = false;
                        for (int n = 0; n < 2; n++)
                        {
                            if (((Creature)player).bodyChunks[n].ContactPoint.x != 0 || ((Creature)player).bodyChunks[n].ContactPoint.y != 0)
                            {
                                player.animation = Player.AnimationIndex.None;
                                player.standing = (((Creature)player).bodyChunks[0].pos.y > ((Creature)player).bodyChunks[1].pos.y);
                                break;
                            }
                        }
                        break;
                    }
                case Player.AnimationIndex.RocketJump:
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        player.standing = false;
                        ((Creature)player).bodyChunks[1].vel *= 0.99f;
                        Vector2 normalized = ((Creature)player).bodyChunks[0].vel.normalized;
                        ((Creature)player).bodyChunks[0].vel += normalized;
                        ((Creature)player).bodyChunks[1].vel -= normalized;
                        BodyChunk bodyChunk69 = ((Creature)player).bodyChunks[0];
                        bodyChunk69.vel.y = bodyChunk69.vel.y + 0.1f;
                        BodyChunk bodyChunk70 = ((Creature)player).bodyChunks[1];
                        bodyChunk70.vel.y = bodyChunk70.vel.y + 0.1f;
                        if (((Creature)player).bodyChunks[1].ContactPoint.x != 0 || ((Creature)player).bodyChunks[1].ContactPoint.y != 0)
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        break;
                    }
                case Player.AnimationIndex.BellySlide:
                    {
                        player.bodyMode = Player.BodyModeIndex.Default;
                        if (player.rollCounter < 6)
                        {
                            BodyChunk bodyChunk71 = ((Creature)player).bodyChunks[1];
                            bodyChunk71.vel.y = bodyChunk71.vel.y + 2.7f;
                            BodyChunk bodyChunk72 = ((Creature)player).bodyChunks[1];
                            bodyChunk72.vel.x = bodyChunk72.vel.x - 9.1f * (float)player.rollDirection;
                        }
                        else if (((Creature)player).IsTileSolid(1, 0, -1) || ((Creature)player).IsTileSolid(1, 0, -2))
                        {
                            BodyChunk bodyChunk73 = ((Creature)player).bodyChunks[1];
                            bodyChunk73.vel.y = bodyChunk73.vel.y - 0.5f;
                        }
                        BodyChunk bodyChunk74 = ((Creature)player).bodyChunks[0];
                        bodyChunk74.vel.x = bodyChunk74.vel.x + ((!player.longBellySlide) ? 18.1f : 14f) * (float)player.rollDirection * Mathf.Sin((float)player.rollCounter / ((!player.longBellySlide) ? 15f : 39f) * 3.14159274f);
                        if (((Creature)player).IsTileSolid(0, 0, -1) || ((Creature)player).IsTileSolid(0, 0, -2))
                        {
                            BodyChunk bodyChunk75 = ((Creature)player).bodyChunks[0];
                            bodyChunk75.vel.y = bodyChunk75.vel.y - 2.3f;
                        }
                        for (int num3 = 0; num3 < 2; num3++)
                        {
                            if (((Creature)player).bodyChunks[num3].ContactPoint.y == 0)
                            {
                                BodyChunk bodyChunk76 = ((Creature)player).bodyChunks[num3];
                                bodyChunk76.vel.x = bodyChunk76.vel.x * player.surfaceFriction;
                            }
                        }
                        if (player.input[0].y < 0 && player.input[0].downDiagonal == 0 && player.input[0].x == 0 && player.rollCounter > 8 && player.room.GetTilePosition(((Creature)player).bodyChunks[0].pos).y == player.room.GetTilePosition(((Creature)player).bodyChunks[1].pos).y)
                        {
                            RWCustom.IntVector2 tilePosition = player.room.GetTilePosition(((Creature)player).mainBodyChunk.pos);
                            if (!player.room.GetTile(tilePosition + new RWCustom.IntVector2(0, -1)).Solid && player.room.GetTile(tilePosition + new RWCustom.IntVector2(-1, -1)).Solid && player.room.GetTile(tilePosition + new RWCustom.IntVector2(1, -1)).Solid)
                            {
                                ((Creature)player).bodyChunks[0].pos = player.room.MiddleOfTile(((Creature)player).bodyChunks[0].pos) + new Vector2(0f, -20f);
                                ((Creature)player).bodyChunks[0].vel = new Vector2(0f, -11f);
                                ((Creature)player).bodyChunks[1].pos = Vector2.Lerp(((Creature)player).bodyChunks[1].pos, ((Creature)player).bodyChunks[0].pos + new Vector2(0f, player.bodyChunkConnections[0].distance), 0.5f);
                                ((Creature)player).bodyChunks[1].vel = new Vector2(0f, -11f);
                                player.animation = Player.AnimationIndex.None;
                                player.standing = false;
                                ((Creature)player).GoThroughFloors = true;
                                player.rollDirection = 0;
                                return;
                            }
                        }
                        if (player.input[0].x != player.rollDirection && player.input[0].downDiagonal != player.rollDirection)
                        {
                            player.exitBellySlideCounter++;
                        }
                        else
                        {
                            player.exitBellySlideCounter = 0;
                        }
                        if (player.longBellySlide)
                        {
                            player.whiplashJump = false;
                        }
                        else if (player.rollCounter > 5 && player.input[0].x == -player.rollDirection)
                        {
                            player.whiplashJump = true;
                        }
                        if ((player.rollCounter > 8 && player.exitBellySlideCounter > ((!player.longBellySlide) ? 6 : 16)) || (player.rollCounter > ((!player.longBellySlide) ? 15 : 39) || (!player.longBellySlide && player.rollCounter > 6 && !((Creature)player).IsTileSolid(0, 0, -1) && !((Creature)player).IsTileSolid(1, 0, -1))) || (player.input[0].jmp && !player.input[1].jmp && player.rollCounter > 0 && player.rollCounter < ((!player.longBellySlide) ? 12 : 34)))
                        {
                            ((Creature)player).bodyChunks[0].vel.y = 0f;
                            ((Creature)player).bodyChunks[1].vel.y = 0f;
                            player.rollDirection = 0;
                            player.animation = Player.AnimationIndex.None;
                            if (player.longBellySlide)
                            {
                                player.standing = true;
                                ((Creature)player).bodyChunks[0].vel.y = 6f;
                                ((Creature)player).bodyChunks[1].vel.y = 4f;
                                player.room.PlaySound(SoundID.Slugcat_Normal_Jump, ((Creature)player).mainBodyChunk.pos, 0.5f, 1f);
                            }
                            else
                            {
                                player.standing = (player.input[0].y == 1 && !((Creature)player).IsTileSolid(0, 0, 1));
                                for (int num4 = 0; num4 < 2; num4++)
                                {
                                    if (Mathf.Abs(((Creature)player).bodyChunks[num4].vel.x) > 8f)
                                    {
                                        ((Creature)player).bodyChunks[num4].vel *= 0.5f;
                                    }
                                }
                                player.slowMovementStun = ((!player.standing) ? 40 : 20);
                                player.room.PlaySound((!player.standing) ? SoundID.Slugcat_Belly_Slide_Finish_Fail : SoundID.Slugcat_Belly_Slide_Finish_Success, ((Creature)player).mainBodyChunk.pos, 1f, 1f);
                            }
                            player.longBellySlide = false;
                        }
                        else
                        {
                            player.standing = false;
                        }
                        break;
                    }
                case Player.AnimationIndex.AntlerClimb:
                    player.bodyMode = Player.BodyModeIndex.Default;
                    player.canJump = 5;
                    break;
                case Player.AnimationIndex.GrapplingSwing:
                    player.bodyMode = Player.BodyModeIndex.Default;
                    player.standing = false;
                    ((Creature)player).mainBodyChunk.vel -= RWCustom.Custom.PerpendicularVector(RWCustom.Custom.DirVec(((Creature)player).mainBodyChunk.pos, player.tubeWorm.tongues[0].AttachedPos)) * (float)player.input[0].x * 0.25f;
                    break;
                case Player.AnimationIndex.ZeroGSwim:
                    {
                        player.dynamicRunSpeed[0] = 0f;
                        player.dynamicRunSpeed[1] = 0f;
                        player.bodyMode = Player.BodyModeIndex.ZeroG;
                        player.standing = false;
                        player.circuitSwimResistance *= Mathf.InverseLerp(((Creature)player).mainBodyChunk.vel.magnitude + ((Creature)player).bodyChunks[1].vel.magnitude, 15f, 9f);
                        for (int num5 = 0; num5 < 2; num5++)
                        {
                            if (player.swimBits[num5] != null && !RWCustom.Custom.DistLess(((Creature)player).mainBodyChunk.pos, player.swimBits[num5].pos, 50f))
                            {
                                player.swimBits[num5] = null;
                            }
                            ((Creature)player).bodyChunks[num5].vel *= Mathf.Lerp(1f, 0.9f, player.circuitSwimResistance);
                            if (((Creature)player).bodyChunks[num5].ContactPoint.x != 0 || ((Creature)player).bodyChunks[num5].ContactPoint.y != 0)
                            {
                                player.canJump = 12;
                                if (((Creature)player).bodyChunks[num5].lastContactPoint.x != ((Creature)player).bodyChunks[num5].ContactPoint.x || ((Creature)player).bodyChunks[num5].lastContactPoint.y != ((Creature)player).bodyChunks[num5].ContactPoint.y)
                                {
                                    player.Blink(5);
                                    player.room.PlaySound(SoundID.Slugcat_Regain_Footing, ((Creature)player).mainBodyChunk);
                                }
                            }
                        }
                        bool flag4 = player.canJump > 0;
                        if (!flag4 && (player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam || player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam))
                        {
                            flag4 = true;
                        }
                        int num6 = 0;
                        while (num6 < 9 && !flag4)
                        {
                            if (player.room.GetTile(((Creature)player).mainBodyChunk.pos + RWCustom.Custom.eightDirectionsAndZero[num6].ToVector2() * 10f).Solid)
                            {
                                flag4 = true;
                            }
                            num6++;
                        }
                        player.swimCycle += 4f / RWCustom.Custom.LerpMap(((Creature)player).mainBodyChunk.vel.magnitude, 0f, 2f, 120f, 60f);
                        if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                        {
                            player.swimCycle += 1f / Mathf.Lerp(2f, 6f, UnityEngine.Random.value);
                            Vector2 vector2 = player.SwimDir(false);
                            ((Creature)player).mainBodyChunk.vel += vector2 * player.circuitSwimResistance * 0.5f;
                            if (flag4)
                            {
                                ((Creature)player).mainBodyChunk.vel += vector2 * 0.2f;
                            }
                            else
                            {
                                ((Creature)player).mainBodyChunk.vel += vector2 * RWCustom.Custom.LerpMap(Vector2.Distance(((Creature)player).mainBodyChunk.vel, ((Creature)player).bodyChunks[1].vel), 1f, 4f, 0.1f, RWCustom.Custom.LerpMap((((Creature)player).mainBodyChunk.vel + ((Creature)player).bodyChunks[1].vel).magnitude, 4f, 8f, 0.15f, 0.1f));
                            }
                            ((Creature)player).bodyChunks[1].vel -= vector2 * 0.1f;
                            for (int num7 = 0; num7 < 5; num7++)
                            {
                                if (player.room.GetTile(((Creature)player).mainBodyChunk.pos + RWCustom.Custom.fourDirectionsAndZero[0].ToVector2() * 15f).AnyBeam)
                                {
                                    ((Creature)player).mainBodyChunk.vel *= 0.8f;
                                    ((Creature)player).mainBodyChunk.vel += vector2 * 0.2f;
                                    break;
                                }
                            }
                            if (player.canJump > 0 && player.wantToJump > 0)
                            {
                                Vector2 vector3 = new Vector2(0f, 0f);
                                int num8 = 1;
                                while (num8 >= 0 && vector3.x == 0f && vector3.y == 0f)
                                {
                                    RWCustom.IntVector2 tilePosition2 = player.room.GetTilePosition(((Creature)player).bodyChunks[num8].pos);
                                    for (int num9 = 0; num9 < 8; num9++)
                                    {
                                        if (player.room.GetTile(tilePosition2 - RWCustom.Custom.eightDirectionsDiagonalsLast[num9]).Solid && ((vector3.x == 0f && vector3.y == 0f) || Vector2.Distance(vector2, RWCustom.Custom.eightDirectionsDiagonalsLast[num9].ToVector2()) < Vector2.Distance(vector3, vector2)))
                                        {
                                            vector3 = RWCustom.Custom.eightDirectionsDiagonalsLast[num9].ToVector2();
                                        }
                                    }
                                    num8--;
                                }
                                if (vector3.x != 0f || vector3.y != 0f)
                                {
                                    vector3 = vector3.normalized;
                                    Vector2 vector4 = Vector2.Lerp(vector2, vector3, 0.5f);
                                    vector4 = Vector2.Lerp(vector4, RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, ((Creature)player).mainBodyChunk.pos), 0.25f);
                                    ((Creature)player).mainBodyChunk.vel = Vector2.ClampMagnitude(((Creature)player).mainBodyChunk.vel + vector4 * 5.4f, 5.4f);
                                    ((Creature)player).bodyChunks[1].vel = Vector2.ClampMagnitude(((Creature)player).bodyChunks[1].vel + vector4 * 5f, 5f);
                                    ((Creature)player).mainBodyChunk.vel += vector3;
                                    player.room.PlaySound(SoundID.Slugcat_Normal_Jump, ((Creature)player).mainBodyChunk);
                                    player.canJump = 0;
                                    player.wantToJump = 0;
                                }
                            }
                            else if (player.wantToJump > 0 && player.curcuitJumpMeter >= 3f)
                            {
                                Vector2 a2 = Vector2.Lerp(vector2, RWCustom.Custom.DirVec(((Creature)player).bodyChunks[1].pos, ((Creature)player).mainBodyChunk.pos), 0.5f);
                                ((Creature)player).mainBodyChunk.vel += a2 * 4.4f * (0.5f + 0.5f * player.circuitSwimResistance);
                                ((Creature)player).bodyChunks[1].vel += a2 * 4f * (0.5f + 0.5f * player.circuitSwimResistance);
                                player.room.PlaySound(SoundID.Slugcat_Normal_Jump, ((Creature)player).mainBodyChunk);
                                player.canJump = 0;
                                player.wantToJump = 0;
                                player.curcuitJumpMeter = -1f;
                            }
                            else if ((player.input[0].ZeroGGamePadIntVec.x != 0 || (player.input[0].ZeroGGamePadIntVec.y != 0 && Mathf.Sign((float)player.input[0].ZeroGGamePadIntVec.y) != Mathf.Sign(((Creature)player).mainBodyChunk.vel.y))) && player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam && (player.input[1].ZeroGGamePadIntVec.x == 0 || !player.room.GetTile(((Creature)player).mainBodyChunk.lastPos).horizontalBeam))
                            {
                                player.room.PlaySound(SoundID.Slugcat_Grab_Beam, ((Creature)player).mainBodyChunk);
                                player.animation = Player.AnimationIndex.ZeroGPoleGrab;
                                player.standing = false;
                            }
                            else if ((player.input[0].ZeroGGamePadIntVec.y != 0 || (player.input[0].ZeroGGamePadIntVec.x != 0 && Mathf.Sign((float)player.input[0].ZeroGGamePadIntVec.x) != Mathf.Sign(((Creature)player).mainBodyChunk.vel.x))) && player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam && (player.input[1].ZeroGGamePadIntVec.y == 0 || !player.room.GetTile(((Creature)player).mainBodyChunk.lastPos).verticalBeam))
                            {
                                player.room.PlaySound(SoundID.Slugcat_Grab_Beam, ((Creature)player).mainBodyChunk);
                                player.animation = Player.AnimationIndex.ZeroGPoleGrab;
                                player.standing = true;
                            }
                        }
                        if (player.swimCycle > 4f)
                        {
                            player.swimCycle = 0f;
                        }
                        player.circuitSwimResistance = 0f;
                        if (player.curcuitJumpMeter >= 0f)
                        {
                            player.curcuitJumpMeter = Mathf.Clamp(player.curcuitJumpMeter - 0.5f, 0f, 4f);
                        }
                        else
                        {
                            player.curcuitJumpMeter = Mathf.Min(player.curcuitJumpMeter + 0.5f, 0f);
                        }
                        break;
                    }
                case Player.AnimationIndex.ZeroGPoleGrab:
                    {
                        player.dynamicRunSpeed[0] = 0f;
                        player.dynamicRunSpeed[1] = 0f;
                        player.bodyMode = Player.BodyModeIndex.ZeroG;
                        ((Creature)player).mainBodyChunk.vel *= RWCustom.Custom.LerpMap(((Creature)player).mainBodyChunk.vel.magnitude, 2f, 5f, 0.7f, 0.3f);
                        bool flag5 = false;
                        if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                        {
                            if (player.input[0].ZeroGGamePadIntVec.x != 0)
                            {
                                player.zeroGPoleGrabDir.x = player.input[0].ZeroGGamePadIntVec.x;
                            }
                            if (player.input[0].ZeroGGamePadIntVec.y != 0)
                            {
                                player.zeroGPoleGrabDir.y = player.input[0].ZeroGGamePadIntVec.y;
                            }
                        }
                        if (!player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam && !player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam)
                        {
                            player.standing = false;
                            player.animation = Player.AnimationIndex.ZeroGSwim;
                        }
                        else
                        {
                            if (!player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam && player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam)
                            {
                                player.standing = true;
                            }
                            else if (player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam && !player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam)
                            {
                                player.standing = false;
                            }
                            else if (player.input[0].ZeroGGamePadIntVec.x != 0 && player.input[0].ZeroGGamePadIntVec.y == 0)
                            {
                                player.standing = false;
                            }
                            else if (player.input[0].ZeroGGamePadIntVec.x == 0 && player.input[0].ZeroGGamePadIntVec.y != 0)
                            {
                                player.standing = true;
                            }
                            if (player.standing)
                            {
                                if (player.room.readyForAI && player.room.aimap.getAItile(((Creature)player).mainBodyChunk.pos + new Vector2(0f, (float)player.input[0].ZeroGGamePadIntVec.y * 20f)).narrowSpace)
                                {
                                    BodyChunk mainBodyChunk2 = ((Creature)player).mainBodyChunk;
                                    mainBodyChunk2.vel.x = mainBodyChunk2.vel.x + (player.room.MiddleOfTile(((Creature)player).mainBodyChunk.pos).x - ((Creature)player).mainBodyChunk.pos.x) * 0.1f;
                                }
                                else
                                {
                                    BodyChunk mainBodyChunk3 = ((Creature)player).mainBodyChunk;
                                    mainBodyChunk3.vel.x = mainBodyChunk3.vel.x + (player.room.MiddleOfTile(((Creature)player).mainBodyChunk.pos).x + 5f * (float)player.zeroGPoleGrabDir.x - ((Creature)player).mainBodyChunk.pos.x) * 0.1f;
                                }
                                if (player.input[0].ZeroGGamePadIntVec.y != 0)
                                {
                                    if (player.room.GetTile(((Creature)player).mainBodyChunk.pos + new Vector2(0f, (float)player.input[0].ZeroGGamePadIntVec.y * 10f)).verticalBeam)
                                    {
                                        BodyChunk mainBodyChunk4 = ((Creature)player).mainBodyChunk;
                                        mainBodyChunk4.vel.y = mainBodyChunk4.vel.y + (float)player.input[0].ZeroGGamePadIntVec.y * 1.05f * player.slugcatStats.poleClimbSpeedFac;
                                        player.animationFrame++;
                                        if (player.animationFrame > 20)
                                        {
                                            player.animationFrame = 0;
                                            player.room.PlaySound(SoundID.Slugcat_Climb_Up_Vertical_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                        }
                                    }
                                    else if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                                    {
                                        flag5 = true;
                                    }
                                }
                                if (!flag5 && player.room.GetTile(((Creature)player).bodyChunks[1].pos).verticalBeam)
                                {
                                    ((Creature)player).bodyChunks[1].vel *= 0.7f;
                                    BodyChunk bodyChunk77 = ((Creature)player).bodyChunks[1];
                                    bodyChunk77.vel.x = bodyChunk77.vel.x + (player.room.MiddleOfTile(((Creature)player).bodyChunks[1].pos).x - 5f * (float)player.zeroGPoleGrabDir.x - ((Creature)player).bodyChunks[1].pos.x) * 0.1f;
                                }
                            }
                            else
                            {
                                if (player.room.readyForAI && player.room.aimap.getAItile(((Creature)player).mainBodyChunk.pos + new Vector2((float)player.input[0].ZeroGGamePadIntVec.x * 20f, 0f)).narrowSpace)
                                {
                                    BodyChunk mainBodyChunk5 = ((Creature)player).mainBodyChunk;
                                    mainBodyChunk5.vel.y = mainBodyChunk5.vel.y + (player.room.MiddleOfTile(((Creature)player).mainBodyChunk.pos).y - ((Creature)player).mainBodyChunk.pos.y) * 0.1f;
                                }
                                else
                                {
                                    BodyChunk mainBodyChunk6 = ((Creature)player).mainBodyChunk;
                                    mainBodyChunk6.vel.y = mainBodyChunk6.vel.y + (player.room.MiddleOfTile(((Creature)player).mainBodyChunk.pos).y + 5f * (float)player.zeroGPoleGrabDir.y - ((Creature)player).mainBodyChunk.pos.y) * 0.1f;
                                }
                                if (player.input[0].ZeroGGamePadIntVec.x != 0)
                                {
                                    if (player.room.GetTile(((Creature)player).mainBodyChunk.pos + new Vector2((float)player.input[0].ZeroGGamePadIntVec.x * 10f, 0f)).horizontalBeam)
                                    {
                                        BodyChunk mainBodyChunk7 = ((Creature)player).mainBodyChunk;
                                        mainBodyChunk7.vel.x = mainBodyChunk7.vel.x + (float)player.input[0].ZeroGGamePadIntVec.x * 1.05f * player.slugcatStats.poleClimbSpeedFac;
                                        player.animationFrame++;
                                        if (player.animationFrame > 20)
                                        {
                                            player.animationFrame = 0;
                                            player.room.PlaySound(SoundID.Slugcat_Climb_Up_Vertical_Beam, ((Creature)player).mainBodyChunk, false, 1f, 1f);
                                        }
                                    }
                                    else if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                                    {
                                        flag5 = true;
                                    }
                                }
                                if (!flag5 && player.room.GetTile(((Creature)player).bodyChunks[1].pos).horizontalBeam)
                                {
                                    ((Creature)player).bodyChunks[1].vel *= 0.7f;
                                    BodyChunk bodyChunk78 = ((Creature)player).bodyChunks[1];
                                    bodyChunk78.vel.y = bodyChunk78.vel.y + (player.room.MiddleOfTile(((Creature)player).bodyChunks[1].pos).y - 5f * (float)player.zeroGPoleGrabDir.y - ((Creature)player).bodyChunks[1].pos.y) * 0.1f;
                                }
                            }
                            if (player.input[0].jmp && !player.input[1].jmp)
                            {
                                if (player.input[0].ZeroGGamePadIntVec.x != 0 || player.input[0].ZeroGGamePadIntVec.y != 0)
                                {
                                    Vector2 a3 = player.SwimDir(true);
                                    if (!flag5 && (!player.room.GetTile(((Creature)player).mainBodyChunk.pos).horizontalBeam || !player.room.GetTile(((Creature)player).mainBodyChunk.pos).verticalBeam))
                                    {
                                        if (player.standing && (float)player.input[0].ZeroGGamePadIntVec.x == 0f)
                                        {
                                            a3.y *= 0.1f;
                                        }
                                        else if (!player.standing && (float)player.input[0].ZeroGGamePadIntVec.y == 0f)
                                        {
                                            a3.x *= 0.1f;
                                        }
                                    }
                                    ((Creature)player).mainBodyChunk.vel = Vector2.ClampMagnitude(((Creature)player).mainBodyChunk.vel + a3 * 5.4f, 5.4f);
                                    ((Creature)player).bodyChunks[1].vel = Vector2.ClampMagnitude(((Creature)player).bodyChunks[1].vel + a3 * 5f, 5f);
                                    player.room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, ((Creature)player).mainBodyChunk);
                                }
                                else
                                {
                                    player.room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, ((Creature)player).mainBodyChunk);
                                }
                                player.standing = false;
                                player.animation = Player.AnimationIndex.ZeroGSwim;
                            }
                            if (player.room.readyForAI && player.room.aimap.getAItile(((Creature)player).mainBodyChunk.pos).narrowSpace && player.room.aimap.getAItile(((Creature)player).mainBodyChunk.pos + new Vector2((float)player.input[0].ZeroGGamePadIntVec.x * 20f, (float)player.input[0].ZeroGGamePadIntVec.y * 20f)).narrowSpace)
                            {
                                player.bodyMode = Player.BodyModeIndex.CorridorClimb;
                                player.animation = Player.AnimationIndex.None;
                            }
                        }
                        break;
                    }
                case Player.AnimationIndex.VineGrab:
                    {
                        player.dynamicRunSpeed[0] = 0f;
                        player.dynamicRunSpeed[1] = 0f;
                        player.bodyMode = Player.BodyModeIndex.Default;
                        Vector2 vector5 = player.SwimDir(true);
                        player.room.climbableVines.VineBeingClimbedOn(player.vinePos, player);
                        if (vector5.magnitude > 0f)
                        {
                            player.vineClimbCursor = Vector2.ClampMagnitude(player.vineClimbCursor + vector5 * RWCustom.Custom.LerpMap(Vector2.Dot(vector5, player.vineClimbCursor.normalized), -1f, 1f, 10f, 3f), 30f);
                            Vector2 a4 = player.room.climbableVines.OnVinePos(player.vinePos);
                            player.vinePos.floatPos += player.room.climbableVines.ClimbOnVineSpeed(player.vinePos, ((Creature)player).mainBodyChunk.pos + player.vineClimbCursor) * Mathf.Lerp(2.1f, 1.5f, player.room.gravity) / player.room.climbableVines.TotalLength(player.vinePos.vine);
                            player.vinePos.floatPos = Mathf.Clamp(player.vinePos.floatPos, 0f, 1f);
                            player.room.climbableVines.PushAtVine(player.vinePos, (a4 - player.room.climbableVines.OnVinePos(player.vinePos)) * 0.05f);
                            if (player.vineGrabDelay == 0)
                            {
                                ClimbableVinesSystem.VinePosition vinePosition = player.room.climbableVines.VineSwitch(player.vinePos, ((Creature)player).mainBodyChunk.pos + player.vineClimbCursor, ((Creature)player).mainBodyChunk.rad);
                                if (vinePosition != null)
                                {
                                    player.vinePos = vinePosition;
                                    player.vineGrabDelay = 10;
                                }
                            }
                            player.animationFrame++;
                            if (player.animationFrame > 30)
                            {
                                player.animationFrame = 0;
                            }
                        }
                        else
                        {
                            player.vineClimbCursor *= 0.8f;
                        }
                    ((Creature)player).mainBodyChunk.vel += player.vineClimbCursor / 190f;
                        ((Creature)player).bodyChunks[1].vel -= player.vineClimbCursor / 190f;
                        Vector2 p2 = player.room.climbableVines.OnVinePos(player.vinePos);
                        if (player.input[0].ZeroGGamePadIntVec.x != 0)
                        {
                            player.zeroGPoleGrabDir.x = player.input[0].ZeroGGamePadIntVec.x;
                        }
                        if (player.input[0].ZeroGGamePadIntVec.y != 0)
                        {
                            player.zeroGPoleGrabDir.y = player.input[0].ZeroGGamePadIntVec.y;
                        }
                        bool flag6 = false;
                        if (player.input[0].jmp && !player.input[1].jmp)
                        {
                            flag6 = true;
                            if (vector5.magnitude > 0f)
                            {
                                ((Creature)player).mainBodyChunk.vel = ((Creature)player).mainBodyChunk.vel + vector5.normalized * 4f;
                                ((Creature)player).bodyChunks[1].vel = ((Creature)player).bodyChunks[1].vel + vector5.normalized * 3.5f;
                                ((Creature)player).mainBodyChunk.vel = Vector2.Lerp(((Creature)player).mainBodyChunk.vel, Vector2.ClampMagnitude(((Creature)player).mainBodyChunk.vel, 5f), 0.5f);
                                ((Creature)player).bodyChunks[1].vel = Vector2.Lerp(((Creature)player).bodyChunks[1].vel, Vector2.ClampMagnitude(((Creature)player).bodyChunks[1].vel, 5f), 0.5f);
                                player.room.climbableVines.PushAtVine(player.vinePos, -vector5.normalized * 15f);
                                player.vineGrabDelay = 10;
                            }
                        }
                        else if (!player.room.climbableVines.VineCurrentlyClimbable(player.vinePos))
                        {
                            flag6 = true;
                            player.vineGrabDelay = 10;
                        }
                        if (!flag6 && RWCustom.Custom.DistLess(((Creature)player).mainBodyChunk.pos, p2, 40f + player.room.climbableVines.VineRad(player.vinePos)))
                        {
                            player.room.climbableVines.ConnectChunkToVine(((Creature)player).mainBodyChunk, player.vinePos, player.room.climbableVines.VineRad(player.vinePos));
                            Vector2 a5 = RWCustom.Custom.PerpendicularVector(player.room.climbableVines.VineDir(player.vinePos));
                            ((Creature)player).bodyChunks[0].vel += a5 * 0.2f * (float)((Mathf.Abs(a5.x) <= Mathf.Abs(a5.y)) ? player.zeroGPoleGrabDir.y : player.zeroGPoleGrabDir.x);
                            if (player.room.gravity == 0f)
                            {
                                Vector2 vector6 = player.room.climbableVines.OnVinePos(new ClimbableVinesSystem.VinePosition(player.vinePos.vine, player.vinePos.floatPos - 20f / player.room.climbableVines.TotalLength(player.vinePos.vine)));
                                Vector2 vector7 = player.room.climbableVines.OnVinePos(new ClimbableVinesSystem.VinePosition(player.vinePos.vine, player.vinePos.floatPos + 20f / player.room.climbableVines.TotalLength(player.vinePos.vine)));
                                if (Vector2.Distance(((Creature)player).bodyChunks[1].pos, vector6) < Vector2.Distance(((Creature)player).bodyChunks[1].pos, vector7))
                                {
                                    ((Creature)player).bodyChunks[0].vel -= Vector2.ClampMagnitude(vector6 - ((Creature)player).bodyChunks[1].pos, 5f) / 20f;
                                    ((Creature)player).bodyChunks[1].vel += Vector2.ClampMagnitude(vector6 - ((Creature)player).bodyChunks[1].pos, 5f) / 20f;
                                }
                                else
                                {
                                    ((Creature)player).bodyChunks[0].vel -= Vector2.ClampMagnitude(vector7 - ((Creature)player).bodyChunks[1].pos, 5f) / 20f;
                                    ((Creature)player).bodyChunks[1].vel += Vector2.ClampMagnitude(vector7 - ((Creature)player).bodyChunks[1].pos, 5f) / 20f;
                                }
                            }
                        }
                        else
                        {
                            player.animation = Player.AnimationIndex.None;
                        }
                        break;
                    }
                case Player.AnimationIndex.Dead:
                    player.bodyMode = Player.BodyModeIndex.Dead;
                    break;
            }
        }

        private void HookPlayerUpdate(On.Player.orig_Update orig, Player player, bool eu)
        {
            if (falkAura == null)
            {
                falkAura = new FalkAura(player);
            }
            if (player.input[0].mp && !player.input[1].mp)
            {
                for (int i = 2; i < player.input.Length; i++)
                {
                    if (player.input[i].mp)
                    {
                        falkAura.SwitchAuraState();
                        break;
                    }
                }
            }
            falkAura.Update();
            orig.Invoke(player, eu);
        }

        public override string DisplayName
        {
            get
            {
                return "Falk";
            }
        }

        public override string Description
        {
            get
            {
                return "Slugcat OC by Precipitator based on the More Slugcats Rivulet and Echoes.";
            }
        }

        /*
        public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
        {
            // Rivulet colour
            //Color color = new Color(0.56863f, 0.8f, 0.94118f);

            // Falk colour
            Color color = new Color(0.27451f, 0.41961f, 0.47059f);
            if (slugcatCharacter == -1)
            {
                return new Color?(color);
            }
            return new Color?(Color.Lerp(baseColor, color, 0.75f));
        }
        */

        public override string StartRoom
        {
            get
            {
                return "SL_S03";
                //return "SL_A04";
                //return "SH_A09";
                //return "FB_44";
            }
        }
        public override float? GetCycleLength()
        {
            return Mathf.Lerp(400f, 800f, UnityEngine.Random.value) / 120f; // Wants it in minutes, not in frames or whatever else the cycleLength value is in.
            /*
            float minutes = Mathf.Lerp(400f, 800f, UnityEngine.Random.value) / 60f;
            int cycleLength = (int)(minutes * 40f * 60f) / 2;
            return cycleLength;
            */
        }

        private AxolotlGills gills;
        private float button_pressed;
        private bool justChanged;
        private FalkAura falkAura;
        private List<Hook> hooks;

        public class AxolotlGills
        {
            public AxolotlGills(PlayerGraphics pGraphics, int startSprite)
            {
                this.pGraphics = pGraphics;
                this.startSprite = startSprite;
                this.rigor = 0.5873646f;

                // Default colours
                //this.baseColor = new Color(0.56863f, 0.8f, 0.94118f);
                //this.effectColor = new Color(0.87451f, 0.17647f, 0.91765f);

                // Falk colours
                this.baseColor = new Color(0.27451f, 0.41961f, 0.47059f);
                this.effectColor = new Color(1f, 0.97255f, 0.63922f);

                float num = 1.310689f;
                this.colored = true;
                this.graphic = 3;
                this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
                int num2 = 3;
                this.scalesPositions = new Vector2[num2 * 2];
                this.scaleObjects = new AxolotlScale[this.scalesPositions.Length];
                this.backwardsFactors = new float[this.scalesPositions.Length];
                float num3 = 0.1542603f;
                float num4 = 0.1759363f;
                for (int i = 0; i < num2; i++)
                {
                    float y = 0.03570603f;
                    float num5 = 0.659981f;
                    float num6 = 0.9722961f;
                    float num7 = 0.3644831f;
                    if (i == 1)
                    {
                        y = 0.02899241f;
                        num5 = 0.76459f;
                        num6 = 0.6056554f;
                        num7 = 0.9129724f;
                    }
                    if (i == 2)
                    {
                        y = 0.02639332f;
                        num5 = 0.7482835f;
                        num6 = 0.7223744f;
                        num7 = 0.4567381f;
                    }
                    for (int j = 0; j < 2; j++)
                    {
                        this.scalesPositions[i * 2 + j] = new Vector2((j != 0) ? num5 : (-num5), y);
                        this.scaleObjects[i * 2 + j] = new AxolotlScale(pGraphics);
                        this.scaleObjects[i * 2 + j].length = Mathf.Lerp(2.5f, 15f, num * num6);
                        this.scaleObjects[i * 2 + j].width = Mathf.Lerp(0.65f, 1.2f, num3 * num);
                        this.backwardsFactors[i * 2 + j] = num4 * num7;
                    }
                }
                this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
                this.spritesOverlap = AxolotlGills.SpritesOverlap.InFront;
            }

            public void Update()
            {
                for (int i = 0; i < this.scaleObjects.Length; i++)
                {
                    Vector2 pos = this.pGraphics.owner.bodyChunks[0].pos;
                    Vector2 pos2 = this.pGraphics.owner.bodyChunks[1].pos;
                    float num = 0f;
                    float num2 = 90f;
                    int num3 = i % (this.scaleObjects.Length / 2);
                    float num4 = num2 / (float)(this.scaleObjects.Length / 2);
                    if (i >= this.scaleObjects.Length / 2)
                    {
                        num = 0f;
                        pos.x += 5f;
                    }
                    else
                    {
                        pos.x -= 5f;
                    }
                    Vector2 a = this.rotateVectorDeg(RWCustom.Custom.DegToVec(0f), (float)num3 * num4 - num2 / 2f + num + 90f);
                    float f = RWCustom.Custom.VecToDeg(this.pGraphics.lookDirection);
                    Vector2 vector = this.rotateVectorDeg(RWCustom.Custom.DegToVec(0f), (float)num3 * num4 - num2 / 2f + num);
                    Vector2 vector2 = Vector2.Lerp(vector, RWCustom.Custom.DirVec(pos2, pos), Mathf.Abs(f));
                    if (this.scalesPositions[i].y < 0.2f)
                    {
                        vector2 -= a * Mathf.Pow(Mathf.InverseLerp(0.2f, 0f, this.scalesPositions[i].y), 2f) * 2f;
                    }
                    vector2 = Vector2.Lerp(vector2, vector, Mathf.Pow(this.backwardsFactors[i], 1f)).normalized;
                    Vector2 vector3 = pos + vector2 * this.scaleObjects[i].length;
                    if (!RWCustom.Custom.DistLess(this.scaleObjects[i].pos, vector3, this.scaleObjects[i].length / 2f))
                    {
                        Vector2 a2 = RWCustom.Custom.DirVec(this.scaleObjects[i].pos, vector3);
                        float num5 = Vector2.Distance(this.scaleObjects[i].pos, vector3);
                        float num6 = this.scaleObjects[i].length / 2f;
                        this.scaleObjects[i].pos += a2 * (num5 - num6);
                        this.scaleObjects[i].vel += a2 * (num5 - num6);
                    }
                    this.scaleObjects[i].vel += Vector2.ClampMagnitude(vector3 - this.scaleObjects[i].pos, 10f) / Mathf.Lerp(5f, 1.5f, this.rigor);
                    this.scaleObjects[i].vel *= Mathf.Lerp(1f, 0.8f, this.rigor);
                    this.scaleObjects[i].ConnectToPoint(pos, this.scaleObjects[i].length, true, 0f, new Vector2(0f, 0f), 0f, 0f);
                    this.scaleObjects[i].Update();
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
                {
                    sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true);
                    sLeaser.sprites[i].scaleY = this.scaleObjects[i - this.startSprite].length / this.graphicHeight;
                    sLeaser.sprites[i].anchorY = 0.1f;
                    if (this.colored)
                    {
                        sLeaser.sprites[i + this.scalesPositions.Length] = new FSprite("LizardScaleB" + this.graphic, true);
                        sLeaser.sprites[i + this.scalesPositions.Length].scaleY = this.scaleObjects[i - this.startSprite].length / this.graphicHeight;
                        sLeaser.sprites[i + this.scalesPositions.Length].anchorY = 0.1f;
                    }
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                if (this.pGraphics.owner == null)
                {
                    return;
                }
                for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
                {
                    Vector2 vector = new Vector2(sLeaser.sprites[9].x + camPos.x, sLeaser.sprites[9].y + camPos.y);
                    float f = 0f;
                    float num = 0f;
                    if (i < this.startSprite + this.scalesPositions.Length / 2)
                    {
                        vector.x -= 5f;
                    }
                    else
                    {
                        num = 180f;
                        vector.x += 5f;
                    }
                    sLeaser.sprites[i].x = vector.x - camPos.x;
                    sLeaser.sprites[i].y = vector.y - camPos.y;
                    sLeaser.sprites[i].rotation = RWCustom.Custom.AimFromOneVectorToAnother(vector, Vector2.Lerp(this.scaleObjects[i - this.startSprite].lastPos, this.scaleObjects[i - this.startSprite].pos, timeStacker)) + num;
                    sLeaser.sprites[i].scaleX = this.scaleObjects[i - this.startSprite].width * Mathf.Sign(f);
                    if (this.colored)
                    {
                        sLeaser.sprites[i + this.scalesPositions.Length].x = vector.x - camPos.x;
                        sLeaser.sprites[i + this.scalesPositions.Length].y = vector.y - camPos.y;
                        sLeaser.sprites[i + this.scalesPositions.Length].rotation = RWCustom.Custom.AimFromOneVectorToAnother(vector, Vector2.Lerp(this.scaleObjects[i - this.startSprite].lastPos, this.scaleObjects[i - this.startSprite].pos, timeStacker)) + num;
                        sLeaser.sprites[i + this.scalesPositions.Length].scaleX = this.scaleObjects[i - this.startSprite].width * Mathf.Sign(f);
                        if (i < this.startSprite + this.scalesPositions.Length / 2)
                        {
                            sLeaser.sprites[i + this.scalesPositions.Length].scaleX *= -1f;
                        }
                    }
                    if (i < this.startSprite + this.scalesPositions.Length / 2)
                    {
                        sLeaser.sprites[i].scaleX *= -1f;
                    }
                }
                for (int j = this.startSprite + this.scalesPositions.Length - 1; j >= this.startSprite; j--)
                {
                    sLeaser.sprites[j].color = this.baseColor;
                    if (this.colored)
                    {
                        sLeaser.sprites[j + this.scalesPositions.Length].color = this.effectColor;
                    }
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                this.palette = palette;
                for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
                {
                    sLeaser.sprites[i].color = this.baseColor;
                    if (this.colored)
                    {
                        sLeaser.sprites[i + this.scalesPositions.Length].color = this.effectColor;
                    }
                }
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Midground");
                }
                newContatiner.RemoveChild(sLeaser.sprites[9]); // Ensure the eyes are rendered on top of the gills.
                for (int i = this.startSprite; i < this.startSprite + this.numberOfSprites; i++)
                {
                    newContatiner.AddChild(sLeaser.sprites[i]);
                }
                newContatiner.AddChild(sLeaser.sprites[9]);
            }

            private Vector2 rotateVectorDeg(Vector2 vec, float degAng)
            {
                degAng *= -0.0174532924f;
                return new Vector2(vec.x * Mathf.Cos(degAng) - vec.y * Mathf.Sin(degAng), vec.x * Mathf.Sin(degAng) + vec.y * Mathf.Cos(degAng));
            }

            public AxolotlScale[] scaleObjects;

            public float[] backwardsFactors;

            public int graphic;

            public float graphicHeight;

            public float rigor;

            public float scaleX;

            public bool colored;

            public Vector2[] scalesPositions;

            public PlayerGraphics pGraphics;

            public int numberOfSprites;

            public int startSprite;

            public RoomPalette palette;

            public AxolotlGills.SpritesOverlap spritesOverlap;

            public Color baseColor;

            public Color effectColor;

            public enum SpritesOverlap
            {
                Behind,
                BehindHead,
                InFront
            }
        }

        public class AxolotlScale : BodyPart
        {
            public AxolotlScale(GraphicsModule cosmetics) : base(cosmetics)
            {
            }

            public override void Update()
            {
                base.Update();
                if (this.owner.owner.room.PointSubmerged(this.pos))
                {
                    this.vel *= 0.5f;
                }
                else
                {
                    this.vel *= 0.9f;
                }
                this.lastPos = this.pos;
                this.pos += this.vel;
            }

            public float length;

            public float width;
        }
    }

    internal class FalkAura
    {
        public FalkAura(Player player)
        {
            this.player = player;
            this.flakes = new List<PlayerFlake>();
            auraRoom = player.room;
            SwitchAuraState();
        }

        public void SwitchAuraState()
        {
            auraActive = !auraActive;
            player.glowing = auraActive;
            PlayerGraphics playerGraphics = (PlayerGraphics)player.graphicsModule;
            if (playerGraphics.lightSource != null)
            {
                if (!player.room.lightSources.Contains(playerGraphics.lightSource))
                {
                    playerGraphics.player.room.AddObject(playerGraphics.lightSource);
                }
                playerGraphics.lightSource.color = auraActive ? new Color(0.258823544f, 0.5137255f, 0.796078444f) : Color.black;
            }

        }

        public void Update()
        {
            if (auraActive)
            {
                if (!auraRoom.BeingViewed || this.flakes.Count == 0)
                {
                    for (int i = 0; i < this.flakes.Count; i++)
                    {
                        this.flakes[i].Destroy();
                    }
                    this.flakes = new List<PlayerFlake>();
                    for (int i = 0; i < 10; i++)
                    {
                        PlayerFlake playerFlake = new PlayerFlake(player, this);
                        this.flakes.Add(playerFlake);
                        player.room.AddObject(playerFlake);
                        playerFlake.active = true;
                        playerFlake.PlaceRandomlyAroundPlayer();
                        playerFlake.savedCamPos = player.room.game.cameras[0].currentCameraPosition;
                        playerFlake.reset = false;
                    }
                    auraRoom = player.room;
                }
                PlayerGraphics playerGraphics = (PlayerGraphics)player.graphicsModule;
                if (playerGraphics.lightSource != null)
                {
                    playerGraphics.lightSource.color = auraActive ? new Color(0.258823544f, 0.5137255f, 0.796078444f) : Color.black;
                }
            }
        }

        public void DisruptorDrawSprites(RoomCamera.SpriteLeaser sLeaser)
        {
            int shieldInt = sLeaser.sprites.Length - 1;
            sLeaser.sprites[shieldInt].x = sLeaser.sprites[9].x;
            sLeaser.sprites[shieldInt].y = sLeaser.sprites[9].y;
            sLeaser.sprites[shieldInt].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[shieldInt].scale = (this.auraActive && this.disruptorActive) ? 8f : 0.01f;
        }

        private List<PlayerFlake> flakes;
        private Player player;
        private Room auraRoom;
        public bool auraActive;
        private bool disruptorActive;
    }

    internal class PlayerFlake : GoldFlakes.GoldFlake
    {
        private FalkAura falkAura;
        public PlayerFlake(Player player, FalkAura falkAura)
        {
            this.player = player;
            this.falkAura = falkAura;
        }

        public override void Update(bool eu)
        {
            if (!this.active && !falkAura.auraActive)
            {
                this.savedCamPos = -1;
                return;
            }
            ((Action)Activator.CreateInstance(typeof(Action), this, typeof(CosmeticSprite).GetMethod("Update").MethodHandle.GetFunctionPointer()/*, eu*/))();
            this.vel *= 0.82f;
            this.vel.y = this.vel.y - 0.25f;
            this.vel += RWCustom.Custom.DegToVec(180f + Mathf.Lerp(-45f, 45f, UnityEngine.Random.value)) * 0.1f;
            this.vel += RWCustom.Custom.DegToVec(this.rot + this.velRotAdd + this.yRot) * Mathf.Lerp(0.1f, 0.25f, UnityEngine.Random.value);
            if (this.room.GetTile(this.pos).Solid && this.room.GetTile(this.lastPos).Solid)
            {
                this.reset = true;
            }
            if (this.reset)
            {
                float radius = 75f;
                this.pos = this.player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-radius, radius, UnityEngine.Random.value), Mathf.Lerp(-radius, radius, UnityEngine.Random.value));
                this.lastPos = this.pos;
                this.ResetMe();
                this.reset = false;
                this.vel *= 0f;
                this.active = falkAura.auraActive && !player.inShortcut;
                return;
            }
            if (this.pos.x < this.room.game.cameras[0].pos.x - 20f)
            {
                this.reset = true;
            }
            if (this.pos.x > this.room.game.cameras[0].pos.x + 1366f + 20f)
            {
                this.reset = true;
            }
            if (this.pos.y < this.room.game.cameras[0].pos.y - 200f)
            {
                this.reset = true;
            }
            if (this.pos.y > this.room.game.cameras[0].pos.y + 768f + 200f)
            {
                this.reset = true;
            }
            if (this.room.game.cameras[0].currentCameraPosition != this.savedCamPos)
            {
                PlaceRandomlyAroundPlayer();
                this.savedCamPos = this.room.game.cameras[0].currentCameraPosition;
            }
            if (!this.room.BeingViewed)
            {
                this.Destroy();
            }
            this.lastRot = this.rot;
            this.rot += this.rotSpeed;
            this.rotSpeed = Mathf.Clamp(this.rotSpeed + Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) / 30f, -10f, 10f);
            this.lastYRot = this.yRot;
            this.yRot += this.yRotSpeed;
            this.yRotSpeed = Mathf.Clamp(this.yRotSpeed + Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) / 320f, -0.05f, 0.05f);
        }

        public void PlaceRandomlyAroundPlayer()
        {
            this.ResetMe();
            this.pos = this.player.bodyChunks[0].pos + new Vector2(Mathf.Lerp(-100f, 100f, UnityEngine.Random.value), Mathf.Lerp(-100f, 100f, UnityEngine.Random.value));
            this.lastPos = this.pos;
        }

        Player player;
    }

}
// ~End Of File