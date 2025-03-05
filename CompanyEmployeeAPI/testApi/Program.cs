var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el flujo de procesamiento de solicitudes HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var companies = new List<Company>();
var employees = new List<Employee>();
var terminatedEmployees = new List<TerminatedEmployee>();

app.MapGet("/companies", () => companies)
    .WithName("GetCompanies")
    .WithOpenApi();

app.MapGet("/companies/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    return company is not null ? Results.Ok(company) : Results.NotFound();
})
.WithName("GetCompanyById")
.WithOpenApi();

app.MapPost("/companies", (Company company) =>
{
    company.Id = companies.Count > 0 ? companies.Max(c => c.Id) + 1 : 1;
    companies.Add(company);
    return Results.Created($"/companies/{company.Id}", company);
})
.WithName("CreateCompany")
.WithOpenApi();

app.MapPut("/companies/{id}", (int id, Company updatedCompany) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound();

    company.Name = updatedCompany.Name;
    return Results.Ok(company);
})
.WithName("UpdateCompany")
.WithOpenApi();

app.MapDelete("/companies/{id}", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound();

    if (employees.Any(e => e.CompanyId == id))
    {
        return Results.BadRequest("No se puede eliminar una empresa con empleados asociados.");
    }

    companies.Remove(company);
    return Results.NoContent();
})
.WithName("DeleteCompany")
.WithOpenApi();

app.MapDelete("/companies/{id}/with-employees", (int id) =>
{
    var company = companies.FirstOrDefault(c => c.Id == id);
    if (company is null) return Results.NotFound();

    employees.RemoveAll(e => e.CompanyId == id);
    companies.Remove(company);
    return Results.NoContent();
})
.WithName("DeleteCompanyWithEmployees")
.WithOpenApi();

app.MapGet("/employees", () => employees)
    .WithName("GetEmployees")
    .WithOpenApi();

app.MapGet("/employees/{id}", (int id) =>
{
    var employee = employees.FirstOrDefault(e => e.Id == id);
    return employee is not null ? Results.Ok(employee) : Results.NotFound();
})
.WithName("GetEmployeeById")
.WithOpenApi();

app.MapPost("/employees", (Employee employee) =>
{
    employee.Id = employees.Count > 0 ? employees.Max(e => e.Id) + 1 : 1;
    employees.Add(employee);
    return Results.Created($"/employees/{employee.Id}", employee);
})
.WithName("CreateEmployee")
.WithOpenApi();

app.MapPut("/employees/{id}", (int id, Employee updatedEmployee) =>
{
    var employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee is null) return Results.NotFound();

    employee.Name = updatedEmployee.Name;
    employee.CompanyId = updatedEmployee.CompanyId;
    employee.HireDate = updatedEmployee.HireDate;
    return Results.Ok(employee);
})
.WithName("UpdateEmployee")
.WithOpenApi();

app.MapDelete("/employees/{id}", (int id) =>
{
    var employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee is null) return Results.NotFound();

    employees.Remove(employee);
    return Results.NoContent();
})
.WithName("DeleteEmployee")
.WithOpenApi();

app.MapPost("/employees/{id}/terminate", (int id) =>
{
    var employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee is null) return Results.NotFound();

    var terminationDate = DateTime.Now;
    var severancePay = CalculateSeverancePay(employee.HireDate, terminationDate);

    var terminatedEmployee = new TerminatedEmployee
    {
        Id = employee.Id,
        Name = employee.Name,
        HireDate = employee.HireDate,
        TerminationDate = terminationDate,
        SeverancePay = severancePay
    };

    terminatedEmployees.Add(terminatedEmployee);
    employees.Remove(employee);

    return Results.Ok(terminatedEmployee);
})
.WithName("TerminateEmployee")
.WithOpenApi();

app.MapGet("/terminated-employees", () => terminatedEmployees)
    .WithName("GetTerminatedEmployees")
    .WithOpenApi();

app.MapGet("/terminated-employees/{name}", (string name) =>
{
    var terminatedEmployee = terminatedEmployees.FirstOrDefault(te => te.Name == name);
    return terminatedEmployee is not null ? Results.Ok(terminatedEmployee) : Results.NotFound();
})
.WithName("GetTerminatedEmployeeByName")
.WithOpenApi();

app.Run();

static decimal CalculateSeverancePay(DateTime hireDate, DateTime terminationDate)
{
    var yearsWorked = (terminationDate - hireDate).Days / 365;
    return yearsWorked * 1000; // Cálculo de ejemplo
}

record Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

record Employee
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CompanyId { get; set; }
    public DateTime HireDate { get; set; }
}

record TerminatedEmployee
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime TerminationDate { get; set; }
    public decimal SeverancePay { get; set; }
}
