using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rubiks
{
    enum Face { front, back };
    public partial class Form1 : Form
    {
        //the change in angle - reset every tick
        Point3D angle = new Point3D(0, 0, 0),
            lightSource = new Point3D(-1000, -1000, 1000),
            axis = new Point3D(0, 0, 0);
        Point2D mouseLastPos = new Point2D();
        Random ran = new Random();
        MouseEventArgs mouseD = new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0);
        List<Polygon3D> allSides = new List<Polygon3D>();
        List<Cube> cubes = new List<Cube>();
        //rotation step is how many steps the animation takes to rotate a side
        double cubeTranslation = 100, rotationStep = 15;
        double size = 90;
        int tick = 0;
        bool canClick = true;

        public Form1()
        {
            InitializeComponent();
        }
        /// <summary>
        /// The reset function is my way out of programming a solving algorhithm. It destroys the old, unsolved cube and just makes a new one
        /// </summary>
        public void Reset()
        {
            angle = new Point3D();
            Global.TotalAngle = new Point3D();
            mouseLastPos = new Point2D();
            allSides.Clear();
            cubes.Clear();
            canClick = true;

            Form1_Load(null, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        //Creates, scales, and positions cubes. This also stores an Original Location of one corner on the cube - used for solving check
                        Cube aCube = new Cube(this.ClientSize, x, y, z);
                        aCube.Scale(size);
                        aCube.Translate(new Point3D(x, y, z) * cubeTranslation, false);
                        aCube.OriginalLocation = aCube.Corner();
                        cubes.Add(aCube);
                        allSides.AddRange(aCube.Sides);
                    }
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    this.Dispose();
                    break;
                //rotation keys
                case Keys.X:
                    if (e.Modifiers == Keys.Shift)
                        angle.X = -3;
                    else
                        angle.X = 3;
                    break;
                case Keys.Y:
                    if (e.Modifiers == Keys.Shift)
                        angle.Y = -3;
                    else
                        angle.Y = 3;
                    break;
                case Keys.Z:
                    if (e.Modifiers == Keys.Shift)
                        angle.Z = -3;
                    else
                        angle.Z = 3;
                    break;
                //Resets the cube
                case Keys.A:
                    Reset();
                    break;
                //scrambles cube
                case Keys.S:
                    Scramble();
                    break;
                //toggle controls box
                case Keys.OemQuestion:
                    if (groupBox1.Visible)
                    {
                        groupBox1.Hide();
                        label8.Show();
                    }
                    else
                    {
                        groupBox1.Show();
                        label8.Hide();
                    }
                    break;
            }
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            //click and drag rotation. Checks current mouse position relative to last position and adjusts rotation angle
            if (mouseD.Button == MouseButtons.Left)
            {
                angle.X = (e.Y - mouseLastPos.Y > 1) ? angle.X = 8 : (e.Y - mouseLastPos.Y < -1) ? angle.X = -8 : angle.X = 0;
                angle.Y = (e.X - mouseLastPos.X > 1) ? angle.Y = -8 : (e.X - mouseLastPos.X < -1) ? angle.Y = 8 : angle.Y = 0;
            }
            //sets current position as "last position"
            mouseLastPos = new Point2D(e.Location.X, e.Location.Y);
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseD = e;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (canClick)
            {
                //calls a checkClick function for mouse position at lift. Changes direction based on whether or not shift is pressed
                if (e.Button.Equals(MouseButtons.Left) && Control.ModifierKeys == Keys.Shift)
                    CheckClick(new Point2D(e.Location.X - ClientSize.Width / 2, e.Location.Y - ClientSize.Height / 2), -1);
                else if (e.Button.Equals(MouseButtons.Left))
                    CheckClick(new Point2D(e.Location.X - ClientSize.Width / 2, e.Location.Y - ClientSize.Height / 2), 1);
            }
            mouseD = new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0);
        }
        private void CheckClick(Point2D mouseLocation, int direction)
        {
            //orders the allSides so that the side you see on top is checked first
            allSides.Sort();
            allSides.Reverse();

            for (int i = 0; i < allSides.Count; i++)
            {
                if (allSides[i].Contains(mouseLocation) && allSides[i].FrontColor != Color.Black)
                {
                    //if a face contains your mouse click (i.e. its in the cube), runs click code
                    CheckClick2(i, direction);
                    break;
                }
            }
        }
        private void CheckClick2(int i, int direction)
        {
            UnRotateAll();
            Point3D sideCenter = allSides[i].Center().toInt();
            if (sideCenter.Magnitude == cubeTranslation + size / 2)
            {
                //finds every cube that belongs to the side that should rotate, flags them 
                sideCenter.Normalize();
                foreach (Cube cube in cubes)
                    if (cube.Center().toInt() * sideCenter == cubeTranslation)
                        cube.RotateFlag = true;

                //determines the rotation of flagged sides
                axis = new Point3D(0, 0, 0);
                if (sideCenter.X != 0)
                    axis.X = (sideCenter.X < 0) ? direction * rotationStep : direction * rotationStep * -1;
                else if (sideCenter.Y != 0)
                    axis.Y = (sideCenter.Y < 0) ? direction * rotationStep : direction * rotationStep * -1;
                else if (sideCenter.Z != 0)
                    axis.Z = (sideCenter.Z > 0) ? direction * rotationStep : direction * rotationStep * -1;
            }

            RotateAll();
        }
        public void Scramble()
        {
            //randomly picks 1000 faces to "click" on - doesn't always rotate as only clicking on the center cube reslts in a rotation
            for (int i = 0; i < 1000; i++)
            {
                int j = ran.Next(0, allSides.Count);
                if (allSides[j].FrontColor != Color.Black)
                {
                    CheckClick2(j, 1);

                    UnRotateAll();
                    //this function does not visualize the scrambling, it was taking too long for my liking. This way just pops out a random cube
                    foreach (Cube c in cubes)
                    {
                        if (c.RotateFlag)
                            c.RotateDeg(axis * (90 / rotationStep));
                        c.RotateFlag = false;
                    }
                    RotateAll();
                }
            }
        }
        public void UnRotateAll()
        {
            foreach (Cube c in cubes)
            {
                c.UnRotateDeg(Global.TotalAngle);
            }
        }
        public void RotateAll()
        {
            foreach (Cube c in cubes)
            {
                c.RotateDeg(Global.TotalAngle);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            //makes center screen 0,0
            gr.TranslateTransform(ClientSize.Width / 2, ClientSize.Height / 2);

            allSides.Sort();
            for (int i = 0; i < allSides.Count; i++)
            {
                //draws every face from the back up
                allSides[i].Draw(gr, Global.Distance, Face.back, lightSource);
                allSides[i].Draw(gr, Global.Distance, Face.front, lightSource);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //first check if the rotation sequence is NOT finsished
            if (tick < (90 / rotationStep) && (axis.X + axis.Y + axis.Z) != 0)
            {
                canClick = false;
                UnRotateAll();
                foreach (Cube c in cubes)
                {
                    if (c.RotateFlag)
                        c.RotateDeg(axis);
                }
                RotateAll();
                tick++;
                //if this tick is the last tick if unflags cubes and stops rotation
                if (tick >= 90 / rotationStep)
                {
                    tick = 0;
                    axis = new Point3D();
                    foreach (Cube c in cubes)
                        c.RotateFlag = false;
                }
            }
            //if not in rotation sequence this runs to add the cube rotation from clicking and dragging to total angle
            else
            {
                canClick = true;
                UnRotateAll();
                Global.TotalAngle += angle;
                angle = new Point3D();
                RotateAll();
                //checks for a solved cube
                CheckSolved();
            }
            //repaint
            this.Invalidate();
        }
        public void CheckSolved()
        {
            bool solved = true;
            UnRotateAll();
            foreach (Cube c in cubes)
            {
                //checks if a corner is in the same position as when it started, does NOT CHECK CENTER CUBE
                if (!c.OriginalLocation.IsEqualTo(c.Corner()) && c.GetFaceColours.Count() > 14)
                {
                    //if one cube does not match it flags the cube as NotSolved
                    solved = false;
                    break;
                }
            }
            RotateAll();
            //changes the text in the top left corner to reflect the cube state
            if (solved)
            {
                label1.Text = "Solved";
                label1.ForeColor = Color.Green;
            }
            else
            {
                label1.Text = "Not Solved";
                label1.ForeColor = Color.Red;
            }
        }
    }
    class Global
    {
        //Created this Global class out of lack of proper programming procedure. It works in this program so I've left it to save time, will remove for future reference

        //the hypothetical distance of the viewers eyes from the screen
        private static double distance = 1000;
        //the total change in angle from the start
        private static Point3D totalAngle = new Point3D();
        //a list of every side that exists. This includes both the grid and every cube
        public static double Distance { get { return distance; } set { distance = value; } }
        public static Point3D TotalAngle { get { return totalAngle; } set { totalAngle = value; } }
    }
}