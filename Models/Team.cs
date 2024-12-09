namespace KlickServer.Models
{
    class Team
    {
        private readonly Guid _id;
        private readonly string _name;
        private readonly int _score;

        public Team(string name)
        {
            _id = Guid.NewGuid();
            _name = name;
            _score = 0;
        }

        public Guid Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public int Score
        {
            get { return _score; }
        }
    }
}
