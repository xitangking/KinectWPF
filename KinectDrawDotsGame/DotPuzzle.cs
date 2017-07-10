using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KinectDrawDotsGame
{
    class DotPuzzle
    {
        public List<Point> Dots { get; set; }

        public DotPuzzle()
        {
            this.Dots = new List<Point>();
        }
    }
}
