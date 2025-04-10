using Tutorial3.Models;
namespace Tutorial3Tests;
using Xunit;
using Xunit.Abstractions;

public class EmpDeptSalgradeTests(ITestOutputHelper output)
{
    // 1. Simple WHERE filter
    // SQL: SELECT * FROM Emp WHERE Job = 'SALESMAN';
    [Fact]
    public void ShouldReturnAllSalesmen()
    {
        var emps = Database.GetEmps();

        var result = emps.Where(e => e.Job == "SALESMAN").ToList(); 
        
        /*wypisanie wyniku 
        foreach (var emp in result)
        {
            output.WriteLine($"{emp.EName} - {emp.Sal} - {emp.Job}");
        }*/

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("SALESMAN", e.Job));
    }
    
    // 2. WHERE + OrderBy
    // SQL: SELECT * FROM Emp WHERE DeptNo = 30 ORDER BY Sal DESC;
    [Fact]
    public void ShouldReturnDept30EmpsOrderedBySalaryDesc()
    {
        var emps = Database.GetEmps();

        var result = emps
            .Where(e => e.DeptNo == 30)
            .OrderByDescending(e => e.Sal)
            .ToList();
        
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Sal >= result[1].Sal);
    }

    // 3. Subquery using LINQ (IN clause)
    // SQL: SELECT * FROM Emp WHERE DeptNo IN (
    //          SELECT DeptNo FROM Dept WHERE Loc = 'CHICAGO'
    // );
    [Fact]
    public void ShouldReturnEmployeesFromChicago()
    {
        var emps = Database.GetEmps();
        var depts = Database.GetDepts();

        var result = emps
            .Where(e => depts.Any(d => d.Loc == "CHICAGO" && d.DeptNo == e.DeptNo))
            .ToList();

        Assert.All(result, e => Assert.Equal(30, e.DeptNo));
    }

    // 4. SELECT projection
    // SQL: SELECT EName, Sal FROM Emp;
    [Fact]
    public void ShouldSelectNamesAndSalaries()
    {
        var emps = Database.GetEmps();

        var result = emps
            .Select(e => new
            {
                e.EName, 
                e.Sal
            })
            .ToList();
        
        Assert.All(result, r =>
        { 
            Assert.False(string.IsNullOrWhiteSpace(r.EName)); 
            Assert.True(r.Sal > 0);
        });
    }

    // 5. JOIN Emp to Dept
    // SQL: SELECT E.EName, D.DName FROM Emp E JOIN Dept D ON E.DeptNo = D.DeptNo;
    [Fact]
    public void ShouldJoinEmployeesWithDepartments()
    {
        var emps = Database.GetEmps();
        var depts = Database.GetDepts();

        var result = emps
            .Join(                                  //na czym robimy join
                depts,                              //co dolaczamy
                emp => emp.DeptNo,                  //klucz emps
                dept => dept.DeptNo,                //klucz depts
                (emp, dept) => new { emp, dept }    //wynik polaczenia 
            ).Select(obj => new           //tylko ename i dname 
            {
                obj.emp.EName,
                obj.dept.DName
            })
            .ToList(); 
        
        Assert.Contains(result, r => r.DName == "SALES" && r.EName == "ALLEN");
    }

    // 6. Group by DeptNo
    // SQL: SELECT DeptNo, COUNT(*) FROM Emp GROUP BY DeptNo;
    [Fact]
    public void ShouldCountEmployeesPerDepartment()
    {
        var emps = Database.GetEmps();

        var result = emps
            .GroupBy(emp => emp.DeptNo)
            .Select(group => new
            {
                DeptNo = group.Key,
                Count = group.Count()       //liczba pracownikow w danej grupie
            })
            .ToList();
        
        Assert.Contains(result, g => g.DeptNo == 30 && g.Count == 2);
    }

    // 7. SelectMany (simulate flattening)
    // SQL: SELECT EName, Comm FROM Emp WHERE Comm IS NOT NULL;
    [Fact]
    public void ShouldReturnEmployeesWithCommission()
    {
        var emps = Database.GetEmps();

        var result = emps
            .Where(emp => emp.Comm is not null)             //ci z prowizja
            .Select(emp => new                              //tylko ename i prowizja
            {
                emp.EName, 
                emp.Comm
            })
            .ToList();
        
        Assert.All(result, r => Assert.NotNull(r.Comm));
    }

    // 8. Join with Salgrade
    // SQL: SELECT E.EName, S.Grade FROM Emp E JOIN Salgrade S ON E.Sal BETWEEN S.Losal AND S.Hisal;
    [Fact]
    public void ShouldMatchEmployeeToSalaryGrade()
    {
        var emps = Database.GetEmps();
        var grades = Database.GetSalgrades();

        var result = emps
            .SelectMany(emp => grades, (emp, sal) => new { emp, sal })
            .Where(pair => pair.emp.Sal >= pair.sal.Losal && pair.emp.Sal <= pair.sal.Hisal)
            .Select(pair => new
            {
                pair.emp.EName,
                pair.sal.Grade
            })
            .ToList();
        
        Assert.Contains(result, r => r.EName == "ALLEN" && r.Grade == 3);
    }

    // 9. Aggregation (AVG)
    // SQL: SELECT DeptNo, AVG(Sal) FROM Emp GROUP BY DeptNo;
    [Fact]
    public void ShouldCalculateAverageSalaryPerDept()
    {
        //emp => emp.Sal to dla kazdego pracownika zwroc jego pensje
        var emps = Database.GetEmps();

        var result = emps
            .GroupBy(emp => emp.DeptNo)             //grupowanie wedlug dzialu
            .Select(group => new
            {
                DeptNo = group.Key,         
                AvgSal = group.Average(emp => emp.Sal)      //srednia pensja w grupie
            })
            .ToList();
        
        Assert.Contains(result, r => r.DeptNo == 30 && r.AvgSal > 1000);
    }

    // 10. Complex filter with subquery and join
    // SQL: SELECT E.EName FROM Emp E WHERE E.Sal > (SELECT AVG(Sal) FROM Emp WHERE DeptNo = E.DeptNo);
    [Fact]
    public void ShouldReturnEmployeesEarningMoreThanDeptAverage()
    {
        var emps = Database.GetEmps();

        // var result = null; 
        //
        // Assert.Contains("ALLEN", result);
    }
}