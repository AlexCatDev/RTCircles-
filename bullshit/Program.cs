using Realms;


class DBObject : RealmObject
{
    [PrimaryKey]
    public int ID { get; set; }

    public IList<string> Keys { get; }
}

class Program
{

    static void Main(string[] args)
    {
        DBObject k = new DBObject();
        k.ID = 0;
        k.Keys.Add("Penis");

        Realm s = Realm.GetInstance("test.db");
        s.Error += (e, x) =>
        {
            Console.WriteLine(x.Exception.Message);
        };

        var obj = s.Find<DBObject>(0).Freeze();

        new Thread(() =>
        {
            Console.WriteLine(obj.ID);
        }).Start();

        Console.ReadLine();
    }
}