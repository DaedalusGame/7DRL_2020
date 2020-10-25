using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class LineSet
    {
        List<Vector2> Points = new List<Vector2>();
        List<Vector2> Pivots = new List<Vector2>();
        List<float> Distances = new List<float>();
        public float TotalDistance;

        public void AddPoint(Vector2 point)
        {
            Points.Add(point);
            Pivots.Add(Vector2.Zero);
            if (Points.Count == 2)
                Pivots[0] = CalculatePivot(Points[1] - Points[0]);
            if (Points.Count > 2)
                CalculatePivot(Points.Count - 2);
            if (Points.Count > 1)
            {
                Vector2 p1 = Points[Points.Count - 2];
                Vector2 p2 = Points[Points.Count - 1];
                Pivots[Points.Count - 1] = CalculatePivot(p2 - p1);
                var distance = (p2 - p1).Length();
                TotalDistance += distance;
                Distances.Add(TotalDistance);
            }
            else
            {
                Distances.Add(0);
            }
        }

        public float GetSlide(int index)
        {
            return Distances[index] / TotalDistance;
        }

        public void GetBeam(float start, float end, List<Vector2> points, List<Vector2> pivots, List<float> distances)
        {
            if (start >= 1 || end <= 0)
                return;
            for(int i = 0; i < Points.Count; i++)
            {
                float slide = GetSlide(i);
                Vector2 point = Points[i];
                float distance = Distances[i];

                if (start > slide)
                {
                    float slideNext = GetSlide(i + 1);
                    if(start <= slideNext)
                    {
                        Vector2 pointNext = Points[i + 1];
                        float distanceNext = Distances[i + 1];
                        float slidePrecise = (start - slide) / (slideNext - slide);
                        points.Add(Vector2.Lerp(point, pointNext, slidePrecise));
                        pivots.Add(CalculatePivot(pointNext - point));
                        distances.Add(MathHelper.Lerp(distance, distanceNext, slidePrecise));
                    }
                }
                else if(end <= slide)
                {
                    float slideLast = GetSlide(i - 1);
                    if (end > slideLast)
                    {
                        Vector2 pointLast = Points[i - 1];
                        float distanceLast = Distances[i - 1];
                        float slidePrecise = (end - slideLast) / (slide - slideLast);
                        points.Add(Vector2.Lerp(pointLast, point, slidePrecise));
                        pivots.Add(CalculatePivot(point - pointLast));
                        distances.Add(MathHelper.Lerp(distanceLast, distance, slidePrecise));
                    }
                }
                else
                {
                    points.Add(point);
                    pivots.Add(Pivots[i]);
                    distances.Add(distance);
                }
            }
        }

        private void CalculatePivot(int index)
        {
            Vector2 p1 = Points[index - 1];
            Vector2 p2 = Points[index + 1];
            Pivots[index] = CalculatePivot(p2 - p1);
        }

        private static Vector2 CalculatePivot(Vector2 p)
        {
            return Vector2.Normalize(p).TurnLeft();
        }
    }
}
