﻿/*
 * The MIT License (MIT)
 * Copyright (c) StarX 2016
 */
using System;
using System.Collections.Generic;
using System.Text;
using CrazyStorm_Player.DirectX;
using SlimDX;
using SlimDX.Direct3D9;
using System.IO;
using System.Drawing;
using CrazyStorm_Player.CrazyStorm;

namespace CrazyStorm_Player
{
    class Player : DirectXFramework
    {
        List<Texture> defaultTextures;
        List<ParticleType> defaultParticleTypes;
        CrazyStorm.File file;
        Vector2 customCenter;
        protected override void OnInitialize()
        {
            WindowTitle = VersionInfo.AppTitle;
            int particleMaximum = Int32.Parse(Environment.GetCommandLineArgs()[2]);
            int curveParticleMaximum = Int32.Parse(Environment.GetCommandLineArgs()[3]);
            ParticleManager.Initialize(WindowWidth, WindowHeight, 50, particleMaximum, curveParticleMaximum);
            if (!bool.Parse(Environment.GetCommandLineArgs()[4]))
                customCenter = new Vector2(Int32.Parse(Environment.GetCommandLineArgs()[5]), 
                    Int32.Parse(Environment.GetCommandLineArgs()[6]));
            ParticleManager.OnParticleDraw += (particle) =>
            {
                if (particle.Type == null)
                    return;

                Vector2 center = new Vector2(WindowWidth / 2, WindowHeight / 2) + customCenter;
                Vector2 imageCenter = new Vector2(particle.Type.CenterPoint.x, particle.Type.CenterPoint.y);
                Vector2 scale = new Vector2(particle.WidthScale, particle.HeightScale);
                Vector2 position = new Vector2(particle.PPosition.x + center.X - imageCenter.X, 
                    particle.PPosition.y + center.Y - imageCenter.Y);
                Sprite.Transform = Matrix.Transformation2D(Vector2.Zero, 0, scale, imageCenter,
                    (float)MathHelper.DegToRad(particle.PRotation), position);
                Color4 color = new Color4(particle.Opacity / 100, particle.RGB.r / 255, particle.RGB.g / 255, particle.RGB.b / 255);
                Rectangle rect = new Rectangle((int)particle.Type.StartPoint.x, (int)particle.Type.StartPoint.y, 
                    particle.Type.Width, particle.Type.Height);
                if (particle.Type.Id >= ParticleType.DefaultTypeIndex)
                    Sprite.Draw(defaultTextures[0], rect, color);
            };
            ParticleManager.OnCurveParticleDraw += (curveParticle) =>
            {
                if (curveParticle.Type == null)
                    return;

                //TODO
            };
        }
        protected override void OnLoad()
        {
            base.OnLoad();
            defaultTextures = new List<Texture>();
            defaultTextures.Add(Texture.FromFile(Device, "Resources/Default/barrages.png", Usage.None, Pool.Managed));
            using (StreamReader reader = new StreamReader("Resources/Default/set.txt"))
            {
                defaultParticleTypes = new List<ParticleType>();
                ParticleType.LoadDefaultTypes(reader, defaultParticleTypes);
                EventManager.DefaultTypes = defaultParticleTypes;
            }
            using (FileStream stream = new FileStream(Environment.GetCommandLineArgs()[1], FileMode.Open))
            {
                var reader = new BinaryReader(stream);
                //Play file use UTF-8 encoding
                string header = PlayDataHelper.ReadString(reader);
                if (header == "BG")
                {
                    string version = PlayDataHelper.ReadString(reader);
                    if (VersionInfo.Version == version)
                    {
                        file = new CrazyStorm.File();
                        file.LoadPlayData(reader);
                        RebuildObjectReference(file);
                    }
                    else
                        throw new NotSupportedException();
                }
            }
            file.ParticleSystems[0].Reset();
        }
        protected override void OnUnLoad()
        {
            base.OnUnLoad();
        }
        protected override void OnUpdate()
        {
            file.ParticleSystems[0].Update();
            ParticleManager.Update();
            EventManager.Update();
        }
        protected override void OnDraw()
        {
            ClearScreen(ClearFlags.Target | ClearFlags.ZBuffer, new Color4(0.3f, 0.3f, 0.3f), 1, 0);
            Sprite.Begin(SpriteFlags.AlphaBlend);
            ParticleManager.Draw();
            Sprite.End();
        }
        void RebuildObjectReference(CrazyStorm.File file)
        {
            foreach (var particleSystem in file.ParticleSystems)
            {
                //Rebuild all custom types
                foreach (var customType in particleSystem.CustomTypes)
                    customType.RebuildReferenceFromCollection(file.Images);
                //Collect all particle types
                var particleTypes = new List<ParticleType>();
                particleTypes.AddRange(defaultParticleTypes);
                particleTypes.AddRange(particleSystem.CustomTypes);
                //Collect all components
                var components = new List<Component>();
                foreach (var layer in particleSystem.Layers)
                    components.AddRange(layer.Components);
                //Rebuild components reference
                foreach (var component in components)
                {
                    component.RebuildReferenceFromCollection(components);
                    //Rebuild particles reference
                    if (component is Emitter)
                        (component as Emitter).Template.RebuildReferenceFromCollection(particleTypes);
                }
            }
        }
    }
}
