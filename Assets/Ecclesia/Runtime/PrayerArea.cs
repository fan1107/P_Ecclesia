namespace Ecclesia
{
    public static class PrayerArea
    {
        // Broad areas along the pews, including the floor between rows.
        // These are prayer availability zones, not movement barriers or seat slots.
        public static bool Contains(float x,float y)
        {
            return y>=6f && y<=20f && ((x>=2f && x<=8f)||(x>=10f && x<=16f));
        }
    }
}
