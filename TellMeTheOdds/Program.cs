using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SienarShoppingSystems
{
    class Program
    {
        static void Main(string[] args)
        {
            var wantedShips = new List<Ship>
            {
                new Ship
                {
                    Name = "X-Wing",
                    Quantity = 5
                }
            };
            var wantedCards = new List<Card>
            {
                new Card
                {
                    Name = "LukeSkywalker",
                    Quantity = 1,
                },
                new Card
                {
                    Name = "WedgeAntilles",
                    Quantity = 1,
                },
                 new Card
                {
                    Name = "RedSquadronExpert",
                    Quantity = 3,
                },
                new Card
                {
                    Name = "IonTorpedo",
                    Quantity = 3
                }
            };

            var constraints = GatherConstraints(wantedShips, wantedCards);
            var targetFunction = GatherTargetFunction();
            var tableau = new Tableau(constraints, targetFunction);
            tableau.Solve();

            Console.WriteLine("Hello World!");
        }


        public static IList<Constraint> GatherConstraints(List<Ship> requiredShips, List<Card> requiredCards)
        {
            //setup contraints 
            var constraints = new List<Constraint>();
            var expansions = ExpansionRepository.GetAllExpansions();
            foreach (var requiredShip in requiredShips)
            {
                var constraint = new Constraint()
                {
                    RightHandSide = -requiredShip.Quantity
                };

                foreach (var expansion in expansions)
                {
                    constraint.ConstraintValues.Add(expansion.Name, -expansion.Ships.FirstOrDefault(x => x.Name == requiredShip.Name)?.Quantity ?? 0);
                }
                constraints.Add(constraint);
            }

            foreach (var requiredCard in requiredCards)
            {
                var constraint = new Constraint()
                {
                    RightHandSide = -requiredCard.Quantity
                };

                foreach (var expansion in expansions)
                {
                    constraint.ConstraintValues.Add(expansion.Name, -expansion.Cards.FirstOrDefault(x => x.Name == requiredCard.Name)?.Quantity ?? 0);
                }
                constraints.Add(constraint);
            }

            for(var i = 0; i < constraints.Count; i++)
            {
                for (var y = 0; y < constraints.Count(); y++)
                    constraints[i].ConstraintValues.Add($"helper{y}", i == y ? 1 : 0);
            }

            return constraints;
        }

        public static Constraint GatherTargetFunction()
        {
            var expansions = ExpansionRepository.GetAllExpansions();
            var constraint = new Constraint();
            constraint.RightHandSide = 0;
            foreach(var expansion in expansions)
            {
                constraint.ConstraintValues.Add(expansion.Name, -expansion.Price);
            }
            return constraint;
        }
    }

    public class Tableau
    {
        public IList<TableauRow> Rows { get; private set; }
        public TableauRow TargetFunction { get; private set; }
        public (int Row, int Column) Pivot { get; private set; }

        public Tableau(IEnumerable<Constraint> constraints, Constraint targetFunction)
        {
            if (!constraints.All(x => x.ConstraintValues.Count() == constraints.First().ConstraintValues.Count))
                throw new ArgumentException("Constraints must all be of same length");

            Rows = constraints.Select(x => new TableauRow(x)).ToList();
            TargetFunction = new TableauRow(targetFunction);
        }

        public void Solve()
        {
            while(!IsSolved())
            {
                //calcualte new pivot
                CalculatePivot();
                var pivotRow = Rows[Pivot.Row];

                //normalize by pivot value
                pivotRow.NormalizeWithPivot(Pivot.Column);

                //set pivot column on other rows to zero
                for(var i = 0; i < Rows.Count; i++)
                {
                    if (i == Pivot.Row)
                        continue;

                    Rows[i].ZeroingByPivotRow(Pivot.Column, pivotRow);
                }
                TargetFunction.ZeroingByPivotRow(Pivot.Column, pivotRow);
            }

            var result = Rows;
        }

        private bool IsSolved()
        {
            return Rows.All(x => x.RightHandSide >= 0);
        }

        private void CalculatePivot()
        {
            var minRHSValue = Rows.Min(x => x.RightHandSide);
            var rowIndex = Rows.IndexOf(Rows.First(x => x.RightHandSide == minRHSValue));
            var pivotRow = Rows[rowIndex];

            var dividedZValues = new List<decimal>();
            for(var i = 0; i < TargetFunction.Entries.Count; i++)
            {
                var value = pivotRow.Entries[i].Value < 0
                    ? TargetFunction.Entries[i].Value / pivotRow.Entries[i].Value
                    : Int32.MaxValue;
                dividedZValues.Add(Math.Abs(value));
            }

            var columnIndex = dividedZValues.IndexOf(dividedZValues.Min());
            Pivot = (rowIndex, columnIndex);
        }
    }

    public class TableauRow
    {
        public IList<TableauEntry> Entries { get; }
        public decimal RightHandSide { get; private set; }

        public TableauRow(Constraint constraint)
        {
            Entries = constraint.ConstraintValues.Select(x => new TableauEntry(x.Key, x.Value)).ToList();
            RightHandSide = constraint.RightHandSide;
        }

        public void NormalizeWithPivot(int pivotIndex)
        {
            var pivotValue = Entries[pivotIndex].Value;
            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                entry.SetValue(pivotValue != 0 ? entry.Value / pivotValue : 0);
            }
            RightHandSide = pivotValue != 0 ? RightHandSide / pivotValue : 0;
        }

        public void ZeroingByPivotRow(int pivotIndex, TableauRow pivotRow)
        {
            var localPivotValue = Entries[pivotIndex].Value;

            if (!pivotRow.IsPivotElementNormalized(pivotIndex))
            {
                pivotRow.NormalizeWithPivot(pivotIndex);
            }

            var pivotModValues = pivotRow.Entries.Select(x => x.Value * -localPivotValue).ToList();
            for(var i = 0; i < Entries.Count(); i++)
            {
                Entries[i].SetValue(Entries[i].Value + pivotModValues[i]);
            }
            RightHandSide += pivotRow.RightHandSide * -localPivotValue;
        }

        public bool IsPivotElementNormalized(int pivotIndex)
        {
            var pivotValue = Entries[pivotIndex].Value;
            return pivotValue == 1 || pivotValue == 0;
        }
    }

    public class TableauEntry
    {
        public string Name { get; }
        public decimal Value { get; private set; }

        public TableauEntry(string name, decimal value)
        {
            Name = name;
            Value = value;
        }

        public void SetValue(decimal value)
        {
            Value = value;
        }
    }

    public class Constraint
    {
        public Dictionary<string, decimal> ConstraintValues = new Dictionary<string, decimal>();
        public int RightHandSide { get; set; }
    }

    public static class ExpansionRepository
    {
        public static List<XWingExpansion> GetAllExpansions()
        {
            #region setupexpansions
            var coreBox = new XWingExpansion
            {
                Name = "CoreBox",
                Price = 39,
                Ships = new List<Ship>
                {
                    new Ship
                    {
                        Name = "X-Wing",
                        Quantity = 1
                    }
                },
                Cards = new List<Card>
                {
                    new Card
                    {
                        Name = "LukeSkywalker",
                        Quantity = 1
                    },
                    new Card
                    {
                        Name = "RedSquadronExpert",
                         Quantity = 2
                    },
                    new Card
                    {
                        Name = "IonTorpedo",
                         Quantity = 2
                    },
                }
            };
            var xwingExpansion = new XWingExpansion
            {
                Name = "Xwing",
                Price = 12,
                Ships = new List<Ship>
                {
                    new Ship
                    {
                        Name = "X-Wing",
                        Quantity = 1
                    }
                },
                Cards = new List<Card>
                {
                    new Card
                    {
                        Name = "WedgeAntilles",
                        Quantity = 1
                    },
                    new Card
                    {
                        Name = "RedSquadronExpert",
                         Quantity = 1
                    },
                    new Card
                    {
                        Name = "IonTorpedo",
                        Quantity = 1
                    }
                }
            };
            #endregion

            var expansions = new List<XWingExpansion>
        {
            coreBox,
            xwingExpansion
        };
            return expansions;
        }
    }

    public class XWingExpansion
    {
        public List<Ship> Ships = new List<Ship>();
        public List<Card> Cards = new List<Card>();
        public decimal Price { get; set; }
        public string Name { get; set; }
    }

    public class Ship
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

    public class Card
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
