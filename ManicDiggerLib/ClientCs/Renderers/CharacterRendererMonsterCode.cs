﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Globalization;

namespace ManicDigger.Renderers
{
    public interface ICharacterRenderer
    {
        string[] Animations();
        double AnimPeriod { get; set; }
        void SetAnimation(string p);
        void DrawCharacter(AnimationState animstate, Vector3 pos, byte heading, byte pitch, bool moves, float dt, int playertexture, AnimationHint animationhint, float playerspeed);
    }
    public class CharacterRendererMonsterCode : ICharacterRenderer
    {
        public ManicDiggerGameWindow game;
        public void Load(List<string> code)
        {
            this.code.Clear();
            for (int i = 0; i < code.Count; i++)
            {
                string s = code[i];
                if (s.Trim().Length == 0
                    || s.StartsWith("//"))
                {
                    continue;
                }
                object[] ss = new List<object>(s.Split(new[] { ',' })).ToArray();
                for (int ii = 0; ii < ss.Length; ii++)
                {
                    ss[ii] = ((string)ss[ii]).Trim();
                    double d;
                    if (double.TryParse((string)ss[ii], NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                    {
                        ss[ii] = d;
                    }
                }
                this.code.Add(ss);
            }
        }
        List<object[]> code = new List<object[]>();
        double animperiod = 0.3; //was 0.8
        double characterlight = 0;
        double outofphase;
        double speed;
        public double AnimPeriod { get { return animperiod; } set { animperiod = value; } }
        #region ICharacterRenderer Members
        public void DrawCharacter(AnimationState animstate, Vector3 pos, byte heading, byte pitch, bool moves, float dt, int playertexture, AnimationHint animationhint, float playerspeed)
        {
            animstate.interp += dt;
        	animstate.speed = playerspeed * 10;
        	speed = animstate.speed;
        	//Caps maximum player arm/leg movement. Fixes "crazy arms" at 10x speed.
        	if (speed > 2)
        	{
        		speed = 2;
        		animstate.speed = speed;
        	}
        	if (animationhint.InVehicle)
            {
                moves = false;
            }
            if (animstate.data == null)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                animstate.data = d;
            }
            var variables = (Dictionary<string, object>)animstate.data;
            double headingdeg = ((double)heading / 256) * 360;
            characterlight = 127 + (heading / 2);
            //keep track of how far neck is turned
            if (!moves)
            {
                if (headingdeg > animstate.lastheading && !(Math.Abs(headingdeg - animstate.lastheading) > 180))
                {
                    animstate.headbodydelta += Math.Abs(headingdeg - animstate.lastheading);
                }
                if (headingdeg < animstate.lastheading && !(Math.Abs(headingdeg - animstate.lastheading) > 180))
                {
                    animstate.headbodydelta -= Math.Abs(headingdeg - animstate.lastheading);
                }
            }
            //slowly realign body when walking straight forward
            if (moves && !(animationhint.leanleft || animationhint.leanright))
            {
                if (animstate.headbodydelta > 0)
                {
                    animstate.headbodydelta -= (500 * dt) * (speed*0.375f);
                    animstate.bodyrotation = headingdeg - animstate.headbodydelta;
                }
                if (animstate.headbodydelta < 0)
                {
                    animstate.headbodydelta += (500 * dt)* (speed*0.375f);
                    animstate.bodyrotation = headingdeg - animstate.headbodydelta;
                }
            }
            //rotate body when strafing
            if (animationhint.leanleft)
            {
                animstate.headbodydelta -= (500 * dt);
                animstate.bodyrotation = headingdeg - animstate.headbodydelta;
            }
            if (animationhint.leanright)
            {
                animstate.headbodydelta += (500 * dt);
                animstate.bodyrotation = headingdeg - animstate.headbodydelta;
            }
            //restrict neck rotation
            if (!(animstate.headbodydelta <= -45) && !(animstate.headbodydelta >= 45))
            {
                animstate.fullbodyrotate = false;
            }
            if (animstate.headbodydelta >= 45)
            {
                animstate.fullbodyrotate = true;
                animstate.headbodydelta = 45;
                animstate.bodyrotation = (headingdeg - animstate.headbodydelta);
            }
            if (animstate.headbodydelta <= -45)
            {
                animstate.fullbodyrotate = true;
                animstate.headbodydelta = -45;
                animstate.bodyrotation = (headingdeg - animstate.headbodydelta);
            }
            animstate.lastheading = headingdeg;
            
            game.GLMatrixModeModelView();
            game.GLPushMatrix();
            game.GLTranslate(pos.X, pos.Y, pos.Z);

            variables["heading"] = (double)heading;
            variables["pitch"] = (double)pitch;
            variables["headingdeg"] = ((double)headingdeg);
            variables["pitchdeg"] = ((double)pitch / 256) * 360;
            variables["updown"] = (double)UpDown(animstate.interp, (float)animperiod);
            variables["limbrotation1"] = (double)LeftLegRotation(animstate.interp, (float)animperiod);
            variables["limbrotation2"] = (double)RightLegRotation(animstate.interp, (float)animperiod);
            variables["skin"] = (double)playertexture;
            variables["dt"] = (double)dt;
            variables["time"] = (double)animstate.interp;
            variables["anim"] = (double)currentanim;
            variables["hintleanleft"] = (double)GetLeanLeft(animationhint.leanleft);
            variables["hintleanright"] = (double)GetLeanRight(animationhint.leanright);
            variables["bodyrotation"] = (double)animstate.bodyrotation;
            variables["fullbodyrotate"] = (double)GetFullBodyRotate(animstate.fullbodyrotate);
            string[] animations = Animations();
            for (int i = 0; i < animations.Length; i++)
            {
                variables[animations[i]] = (double)i;
            }
            int skinsizex = 64;
            int skinsizey = 32;
            int pc = 0;
            for (; ; )
            {
                if (pc >= code.Count)
                {
                    break;
                }
                object[] ss = code[pc];
                if (ss.Length > 0)
                {
                    switch ((string)ss[0])
                    {
                        case "set":
                            {
                                variables[(string)ss[1]] = (double)getval(ss[2], variables);
                            }
                            break;
                        case "pushmatrix":
                            {
                                game.GLPushMatrix();
                            }
                            break;
                        case "popmatrix":
                            {
                                game.GLPopMatrix();
                            }
                            break;
                        case "mul":
                            {
                                variables[(string)ss[1]] = (double)variables[(string)ss[1]] * getval(ss[2], variables);
                            }
                            break;
                        case "add":
                            {
                                variables[(string)ss[1]] = (double)variables[(string)ss[1]] + getval(ss[2], variables);
                            }
                            break;
                        case "rotate":
                            {
                                game.GLRotate(
                                    getval(ss[1], variables),
                                    getval(ss[2], variables),
                                    getval(ss[3], variables),
                                    getval(ss[4], variables));
                            }
                            break;
                        case "translate":
                            {
                                game.GLTranslate(
                                    getval(ss[1], variables),
                                    getval(ss[2], variables),
                                    getval(ss[3], variables));
                            }
                            break;
                        case "scale":
                            {
                                game.GLScale(
                                    getval(ss[1], variables),
                                    getval(ss[2], variables),
                                    getval(ss[3], variables));
                            }
                            break;
                        case "makecoords":
                            {
                                RectangleF[] coords = CuboidNet(
                                   (float)getval(ss[2], variables),
                                   (float)getval(ss[3], variables),
                                   (float)getval(ss[4], variables),
                                   (float)getval(ss[5], variables),
                                   (float)getval(ss[6], variables));
                                CuboidNetNormalize(coords, skinsizex, skinsizey);
                                variables[(string)ss[1]] = coords;
                            }
                            break;
                        case "drawcuboid":
                            {
                                DrawCuboid(
                                   new Vector3((float)getval(ss[1], variables),
                                    (float)getval(ss[2], variables),
                                    (float)getval(ss[3], variables)),
                                   new Vector3((float)getval(ss[4], variables),
                                    (float)getval(ss[5], variables),
                                    (float)getval(ss[6], variables)),
                                   (int)getval(ss[7], variables),
                                   (RectangleF[])variables[(string)ss[8]]
                                    );
                            }
                            break;
                        case "skinsize":
                            {
                                skinsizex = (int)getval(ss[1], variables);
                                skinsizey = (int)getval(ss[2], variables);
                            }
                            break;
                        case "dim":
                            {
                                if (!variables.ContainsKey((string)ss[1]))
                                {
                                    variables[(string)ss[1]] = (double)getval(ss[2], variables);
                                }
                            }
                            break;
                        case "fun":
                            {
                                if ((string)ss[2] == "tri")
                                {
                                    variables[(string)ss[1]] = (double)TriWave(getval(ss[3], variables));
                                }
                                if ((string)ss[2] == "sin")
                                {
                                    variables[(string)ss[1]] = (double)Math.Sin(getval(ss[3], variables));
                                }
                                if ((string)ss[2] == "abs")
                                {
                                    variables[(string)ss[1]] = (double)Math.Abs(getval(ss[3], variables));
                                }
                            }
                            break;
                        case "ifeq":
                            {
                                if (variables.ContainsKey((string)ss[1])
                                    && (double)variables[(string)ss[1]] != (double)getval(ss[2], variables))
                                {
                                    //find endif
                                    for (int i = pc; i < code.Count; i++)
                                    {
                                        if ((string)(code[i][0]) == "endif")
                                        {
                                            pc = i;
                                            goto next;
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                pc++;
            next:
                ;
            }
            game.GLPopMatrix();
        }
        public static float Normalize(float p, float period)
        {
            return (float)(p % period);//(2 * Math.PI * period));
        }
        float UpDown(float time, float period)
        {
            //float jumpheight = 0.10f;
            //return (float)TriWave(2 * Math.PI * time / (period / 2)) * jumpheight + jumpheight / 2;
            float jumpheight = 0.025f;
            return (float)Math.Sin(2 * Math.PI * time / (period / 2)) * jumpheight + jumpheight / 2;
        }
        float LeftLegRotation(float time, float period)
        {
            //return (float)TriWave(2 * Math.PI * time / period) * 90;
            outofphase = Math.PI;
            return (float)(((Math.Cos(2 * Math.PI * time / 0.5 + outofphase) * speed) * 30));// *speed
        }
        float RightLegRotation(float time, float period)
        {
            //return (float)TriWave(2 * Math.PI * time / period + Math.PI) * 90;
            return (float)((Math.Cos(2 * Math.PI * time / 0.5) * speed) * 30);
        }
        float GetLeanLeft(bool leaning)
        {
            if (leaning)
            { return 1; }
            else { return 0; }
        }
        float GetLeanRight(bool leaning)
        {
            if (leaning)
            { return 1; }
            else { return 0; }
        }
        float GetFullBodyRotate(bool fullbodyrotate)
        {
            if (fullbodyrotate)
            { return 1; }
            else { return 0; }
        }
        private float TriWave(double t)
        {
            double period = 2 * Math.PI;
            t += Math.PI / 2;
            return (float)Math.Abs(2f * (t / period - Math.Floor(t / period + 0.5f))) * 2 - 1;
        }
        private float getval(object ss2, Dictionary<string, object> variables)
        {
            if (ss2 is double)
            {
                return (float)((double)ss2);
            }
            else
            {
                return (float)((double)variables[(string)ss2]);
            }
        }
        double ParseDouble(string s)
        {
            return double.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture);
        }
        #endregion
        //Maps description of position of 6 faces
        //of a single cuboid in texture file to UV coordinates (in pixels)
        //(one RectangleF in texture file for each 3d face of cuboid).
        //Arguments:
        // Size (in pixels) in 2d cuboid net.
        // Start position of 2d cuboid net in texture file.
        public RectangleF[] CuboidNet(float tsizex, float tsizey, float tsizez, float tstartx, float tstarty)
        {
            RectangleF[] coords = new[]
            {
                new RectangleF(tsizez+tstartx,tsizez+tstarty,tsizex,tsizey),//front
                new RectangleF(2*tsizez+tsizex+tstartx,tsizez+tstarty,tsizex,tsizey),//back
                new RectangleF(0+tstartx,tsizez+tstarty,tsizez,tsizey),//left
                new RectangleF(tsizez+tsizex+tstartx,tsizez+tstarty,tsizez,tsizey),//right
                new RectangleF(tsizez+tstartx,0+tstarty,tsizex,tsizez),//top
                new RectangleF(tsizez+tsizex+tstartx,0+tstarty,tsizex,tsizez),//bottom
            };
            return coords;
        }
        //Divides CuboidNet() result by texture size, to get relative coordinates. (0-1, not 0-32 pixels).
        public static void CuboidNetNormalize(RectangleF[] coords, float texturewidth, float textureheight)
        {
            for (int i = 0; i < coords.Length; i++)
            {
                coords[i] = new RectangleF((coords[i].X / (float)texturewidth), (coords[i].Y / (float)textureheight),
                    (coords[i].Width / (float)texturewidth), (coords[i].Height / (float)textureheight));
            }
        }
        public void DrawCuboid(Vector3 pos, Vector3 size, int textureid, RectangleF[] texturecoords)
        {
            //Todo: Immediate mode is slow. Maybe use display list or vertex array?

            //front
            //GL.Color3(Color.White);
            GL.BindTexture(TextureTarget.Texture2D, textureid);
            GL.Disable(EnableCap.CullFace);
            GL.Begin(BeginMode.Quads);
            RectangleF rect;
            //front
            rect = texturecoords[0];
            GL.TexCoord2(rect.X, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Y);
            GL.Vertex3(pos.X, pos.Y + size.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X, rect.Y);
            GL.Vertex3(pos.X, pos.Y + size.Y, pos.Z);
            //back
            rect = texturecoords[1];
            GL.TexCoord2(rect.X, rect.Bottom);
            GL.Vertex3(pos.X + size.X, pos.Y, pos.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Bottom);
            GL.Vertex3(pos.X + size.X, pos.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y + size.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y + size.Y, pos.Z);
            //left
            rect = texturecoords[2];
            GL.TexCoord2(rect.X, rect.Bottom);
            GL.Vertex3(pos.X + size.X, pos.Y, pos.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Y);
            GL.Vertex3(pos.X, pos.Y + size.Y, pos.Z);
            GL.TexCoord2(rect.X, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y + size.Y, pos.Z);
            //right
            rect = texturecoords[3];
            GL.TexCoord2(rect.X + rect.Width, rect.Bottom);
            GL.Vertex3(pos.X + size.X, pos.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X, rect.Y);
            GL.Vertex3(pos.X, pos.Y + size.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y + size.Y, pos.Z + size.Z);
            //top
            rect = texturecoords[4];
            GL.TexCoord2(rect.X, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y + size.Y, pos.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y + size.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y + size.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y + size.Y, pos.Z);
            //bottom
            rect = texturecoords[5];
            GL.TexCoord2(rect.X, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Bottom);
            GL.Vertex3(pos.X, pos.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X + rect.Width, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y, pos.Z + size.Z);
            GL.TexCoord2(rect.X, rect.Y);
            GL.Vertex3(pos.X + size.X, pos.Y, pos.Z);

            GL.End();
            GL.Enable(EnableCap.CullFace);
        }
        public string[] Animations()
        {
            List<string> availableanimations = new List<string>();
            for (int i = 0; i < code.Count; i++)
            {
                if ((string)(code[i][0]) == "exportanim" && code[i].Length > 1)
                {
                    string name = (string)(code[i][1]);
                    if (!availableanimations.Contains(name))
                    {
                        availableanimations.Add(name);
                    }
                }
            }
            return availableanimations.ToArray();
        }
        public int currentanim;
        public void SetAnimation(string p)
        {
            currentanim = new List<string>(Animations()).IndexOf(p);
        }
    }
}