namespace Ecclesia
{
    // Coordinates match the orthographic courtyard artwork (18 x 32 world units).
    public static class CourtyardLayout
    {
        public static void Apply(WalkGrid grid)
        {
            for(int x=0;x<WalkGrid.Width;x++)for(int y=0;y<WalkGrid.Height;y++)
                grid.Set(x,y,x>=2 && x<=15 && y>=1 && y<=18);

            // The church facade is solid; the central steps lead to its doorway.
            Fill(grid,7,19,10,19,true);
            // Stone garden wall, with a four-cell central opening.
            Fill(grid,2,14,6,14,false);
            Fill(grid,11,14,15,14,false);
            // Bench and noticeboard footprints; lawns remain freely walkable.
            Fill(grid,3,16,5,17,false);
            Fill(grid,13,16,15,18,false);
            // Large flowering shrubs in the bottom corners.
            Fill(grid,2,1,4,3,false);
            Fill(grid,13,1,15,3,false);
        }

        static void Fill(WalkGrid grid,int left,int bottom,int right,int top,bool open)
        {
            for(int x=left;x<=right;x++)for(int y=bottom;y<=top;y++)grid.Set(x,y,open);
        }
    }
}
