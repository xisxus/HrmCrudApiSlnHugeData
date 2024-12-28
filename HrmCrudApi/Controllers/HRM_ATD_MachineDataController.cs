using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Caching.Memory;
using HrmCrudApi.Models;
using System.Linq;

namespace HRM_Practise.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HRM_ATD_MachineDataController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public HRM_ATD_MachineDataController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/HRM_ATD_MachineData/paginated
        [HttpGet("paginated")]
        public IActionResult GetPaginatedData([FromQuery] int draw, [FromQuery] int start, [FromQuery] int length)
        {
            var allData = _context.HRM_ATD_MachineDatas.Take(1000);
            var filteredData = allData;
            var paginatedData = filteredData
                .Skip(start)
                .Take(length)
                .ToList();

            return Ok(new
            {
                draw,
                recordsTotal = allData.Count(),
                recordsFiltered = filteredData.Count(),
                data = paginatedData
            });
        }

        // GET: api/HRM_ATD_MachineData/search
        [HttpPost("search")]
        public async Task<IActionResult> GetMachineDataWithDate( DataTableParameters2 parameters2)
        {

            try
            {
                var draw = parameters2.Draw;
                var start = parameters2.Start;
                var length = parameters2.Length;
                var sortColumn = "";
                var sortColumnDirection = "";

                foreach (var item in parameters2.Order)
                {
                    sortColumnDirection = item.Dir;
                    sortColumn = item.Name;
                }

                var searchValue = parameters2.Search.Value;

                // Define page size and skip for pagination
                int pageSize = length ?? 0;
                int skip = start ?? 0;
                int recordsTotal = 0;

                // Initial query
                var query = _context.HRM_ATD_MachineDatas.AsQueryable();

                // Optimize Date Filtering
                if (!string.IsNullOrEmpty(parameters2.StartDate))
                {
                    int commonDay = 1, commonMonth = 1, commonYear = 1;

                    // Process the StartDate if needed
                    if (int.TryParse(parameters2.StartDate, out int dateTemp))
                    {
                        if (parameters2.StartDate.Length >= 3)
                            commonYear = dateTemp;
                        else
                        {
                            commonDay = dateTemp;
                            if (dateTemp <= 12)
                                commonMonth = dateTemp;
                        }
                    }
                    else
                    {
                        var resultDate = ExtractDateComponents(parameters2.StartDate);
                        commonYear = Convert.ToInt16(resultDate.Year);
                        commonMonth = Convert.ToInt16(resultDate.Month);
                        commonDay = Convert.ToInt16(resultDate.Day);
                    }

                    // Apply date filter directly
                    if (commonDay != 1 || commonYear != 1 || commonMonth != 1)
                    {
                        var dateFilter = new DateTime(commonYear, commonMonth, commonDay);
                        query = query.Where(m => m.Date == dateFilter);
                    }
                }

                // Filter by MachineId if provided
                if (!string.IsNullOrEmpty(parameters2.MachineId))
                {
                    query = query.Where(m => m.MachineId.Contains(parameters2.MachineId));
                }

                // Apply Search Filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(m =>
                        (m.FingerPrintId != null && m.FingerPrintId.Contains(searchValue)) ||
                        (m.MachineId != null && m.MachineId.Contains(searchValue)) ||
                        (m.HOALR != null && m.HOALR.Contains(searchValue))
                    );
                }

                // Get total records count for pagination (before applying sorting)
                recordsTotal = await query.CountAsync();

                // Apply Sorting (only after all filters are applied)
                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    query = sortColumn.ToLower() switch
                    {
                        "fingerprint" => sortColumnDirection.ToLower() == "asc"
                            ? query.OrderBy(m => m.FingerPrintId)
                            : query.OrderByDescending(m => m.FingerPrintId),
                        "machineid" => sortColumnDirection.ToLower() == "asc"
                            ? query.OrderBy(m => m.MachineId)
                            : query.OrderByDescending(m => m.MachineId),
                        "date" => sortColumnDirection.ToLower() == "asc"
                            ? query.OrderBy(m => m.Date)
                            : query.OrderByDescending(m => m.Date),
                        _ => sortColumnDirection.ToLower() == "asc"
                            ? query.OrderBy(m => m.AutoId)
                            : query.OrderByDescending(m => m.AutoId)
                    };
                }

                // Apply Pagination
                var data = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        m.AutoId,
                        m.FingerPrintId,
                        m.MachineId,
                        Date = m.Date.ToString("yyyy-MM-dd"),
                        Time = m.Time.ToString("HH:mm:ss"),
                        m.Latitude,
                        m.Longitude,
                        m.HOALR
                    })
                    .ToListAsync();

                // Get filtered records count
                var recordsFiltered = await query.CountAsync();

                return Ok(new
                {
                    recordsFiltered = recordsFiltered,
                    recordsTotal = recordsTotal,
                    data = data
                });
            }
            catch (Exception ex)
            {
                // Log the exception here
                return Ok(new
                {
                    draw = "0",
                    recordsFiltered = 0,
                    recordsTotal = 0,
                    data = new List<object>(),
                    error = "An error occurred while processing your request."
                });
            }







            //try
            //{
            //    var query = _context.HRM_ATD_MachineDatas.AsNoTracking().AsQueryable();
            //    int commonDay = 1, commonMonth = 1, commonYear = 1;

            //    if (!string.IsNullOrEmpty(parameters2.StartDate))
            //    {
            //        var dateResult = ExtractDateComponents(parameters2.StartDate);
            //        if (dateResult.Success)
            //        {
            //            commonDay = dateResult.Day;
            //            commonMonth = dateResult.Month;
            //            commonYear = dateResult.Year ?? 1;
            //        }
            //        else if (int.TryParse(parameters2.StartDate, out int dateTemp))
            //        {
            //            if (parameters2.StartDate.Length >= 3)
            //            {
            //                commonYear = dateTemp;
            //            }
            //            else
            //            {
            //                commonDay = dateTemp;
            //                if (dateTemp <= 12)
            //                {
            //                    commonMonth = dateTemp;
            //                }
            //            }
            //        }
            //    }

            //    // Apply date filters
            //    if (commonDay != 1 || commonYear != 1 || commonMonth != 1)
            //    {
            //        ApplyDateFilters(ref query, commonDay, commonMonth, commonYear);
            //    }

            //    // Apply search
            //    if (!string.IsNullOrEmpty(parameters2.Search?.Value))
            //    {
            //        query = ApplySearch(query, parameters2.Search.Value);
            //    }

            //    // Apply sorting
            //    if (parameters2.Order?.Any() == true)
            //    {
            //        query = ApplySorting(query, parameters2.Order.First());
            //    }

            //    var recordsTotal = await query.CountAsync();
            //    var recordsFiltered = recordsTotal;

            //    var data = await query
            //        .Skip((int)parameters2.Start)
            //        .Take((int)parameters2.Length)
            //        .Select(m => new
            //        {
            //            m.AutoId,
            //            m.FingerPrintId,
            //            m.MachineId,
            //            Date = m.Date.ToString("yyyy-MM-dd"),
            //            Time = m.Time.ToString("HH:mm:ss"),
            //            m.Latitude,
            //            m.Longitude,
            //            m.HOALR
            //        })
            //        .ToListAsync();

            //    return Ok(new
            //    {
            //        recordsFiltered,
            //        recordsTotal,
            //        data
            //    });
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { error = "An error occurred while processing your request." });
            //}
        }

        // GET: api/HRM_ATD_MachineData/procedure
        [HttpGet("procedure")]
        public async Task<IActionResult> GetMachineDataFromProcedure([FromQuery] DataTableParameters parameters)
        {
            try
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var data = new List<object>();
                int totalRecords = 0, filteredRecords = 0;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetMachineData", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SetupProcedureParameters(command, parameters);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                data.Add(MapReaderToData(reader));
                            }
                        }

                        totalRecords = (int)command.Parameters["@TotalRecords"].Value;
                        filteredRecords = (int)command.Parameters["@FilteredRecords"].Value;
                    }
                }

                return Ok(new { recordsFiltered = filteredRecords, recordsTotal = totalRecords, data });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while processing your request." });
            }
        }

        // GET: api/HRM_ATD_MachineData/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<HRM_ATD_MachineData>> GetMachineData(int id)
        {
            var machineData = await _context.HRM_ATD_MachineDatas.FindAsync(id);
            if (machineData == null)
            {
                return NotFound();
            }
            return machineData;
        }

        // POST: api/HRM_ATD_MachineData
        [HttpPost]
        public async Task<ActionResult<HRM_ATD_MachineData>> CreateMachineData(HRM_ATD_MachineData machineData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.HRM_ATD_MachineDatas.Add(machineData);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMachineData), new { id = machineData.AutoId }, machineData);
        }

        // PUT: api/HRM_ATD_MachineData/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMachineData(int id, HRM_ATD_MachineData machineData)
        {
            if (id != machineData.AutoId)
            {
                return BadRequest();
            }

            _context.Entry(machineData).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MachineDataExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/HRM_ATD_MachineData/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMachineData(int id)
        {
            var machineData = await _context.HRM_ATD_MachineDatas.FindAsync(id);
            if (machineData == null)
            {
                return NotFound();
            }

            _context.HRM_ATD_MachineDatas.Remove(machineData);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #region Helper Methods
        private static (bool Success, int Day, int Month, int? Year) ExtractDateComponents(string dateString)
        {
            string[] formats = {
                "d/M/yyyy", "d-M-yyyy", "dd/MM/yyyy", "dd-MM-yyyy",  "d/M/", "d-M-", "dd/MM/", "dd-MM-",
                "M/d/yyyy", "M-d-yyyy", "MM/dd/yyyy", "MM-dd-yyyy",  "M/d/", "M-d-", "MM/dd/", "MM-dd-",
                "d/M", "d-M", "dd/MM", "dd-MM",
                "M/d", "M-d", "MM/dd", "MM-dd",
                "d", "M",
                "yyyy/M/d", "yyyy-M-d", "yyyy/MM/dd", "yyyy-MM-dd",  "yyyy/M/", "yyyy-M-", "yyyy/MM/", "yyyy-MM-",
                "yyyy/MM", "yyyy-MM"
            };

            if (DateTime.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture,
                                     System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return (true, parsedDate.Day, parsedDate.Month, parsedDate.Year != 1 ? parsedDate.Year : null);
            }

            return (false, 0, 0, null);
        }

        private void ApplyDateFilters(ref IQueryable<HRM_ATD_MachineData> query, int day, int month, int year)
        {
            if (day != 1 && month != 1 && year != 1)
            {
                var date = new DateTime(year, month, day);
                query = query.Where(m => m.Date == date);
            }
            else if (day != 1 || month != 1)
            {
                var dateOnly = new DateOnly(year, month, day);
                if (month != 1)
                {
                    query = query.Where(m => m.Date.Month == dateOnly.Month || m.Date.Day == dateOnly.Day);
                }
                else
                {
                    query = query.Where(m => m.Date.Day == dateOnly.Day);
                }
            }
            else if (year != 1)
            {
                var yearStr = year.ToString();
                if (yearStr.Length == 3)
                {
                    query = query.Where(m => m.Date.Year.ToString().StartsWith(yearStr));
                }
                else
                {
                    query = query.Where(m => m.Date.Year == year);
                }
            }
        }

        private IQueryable<HRM_ATD_MachineData> ApplySearch(IQueryable<HRM_ATD_MachineData> query, string searchValue)
        {
            return query.Where(m =>
                (m.FingerPrintId != null && m.FingerPrintId.Contains(searchValue)) ||
                (m.MachineId != null && m.MachineId.Contains(searchValue)) ||
                (m.HOALR != null && m.HOALR.Contains(searchValue)));
        }

        private IQueryable<HRM_ATD_MachineData> ApplySorting(IQueryable<HRM_ATD_MachineData> query, Order order)
        {
            return order.Name.ToLower() switch
            {
                "fingerprint" => order.Dir.ToLower() == "asc"
                    ? query.OrderBy(m => m.FingerPrintId)
                    : query.OrderByDescending(m => m.FingerPrintId),
                "machineid" => order.Dir.ToLower() == "asc"
                    ? query.OrderBy(m => m.MachineId)
                    : query.OrderByDescending(m => m.MachineId),
                "date" => order.Dir.ToLower() == "asc"
                    ? query.OrderBy(m => m.Date)
                    : query.OrderByDescending(m => m.Date),
                _ => order.Dir.ToLower() == "asc"
                    ? query.OrderBy(m => m.AutoId)
                    : query.OrderByDescending(m => m.AutoId)
            };
        }

        private void SetupProcedureParameters(SqlCommand command, DataTableParameters parameters)
        {
            //command.Parameters.AddWithValue("@StartDate", parameters.StartDate ?? (object)DBNull.Value);
            //command.Parameters.AddWithValue("@EndDate", parameters.EndDate ?? (object)DBNull.Value);
            //command.Parameters.AddWithValue("@SearchValue", string.IsNullOrEmpty(parameters.Search?.Value) ? DBNull.Value : parameters.Search.Value);
            //command.Parameters.AddWithValue("@SortColumn", parameters.Order?.FirstOrDefault()?.Name ?? DBNull.Value);
            //command.Parameters.AddWithValue("@SortDirection", parameters.Order?.FirstOrDefault()?.Dir ?? "ASC");
            //command.Parameters.AddWithValue("@Start", parameters.Start);
            //command.Parameters.AddWithValue("@Length", parameters.Length);

            //command.Parameters.Add(new SqlParameter("@TotalRecords", SqlDbType.Int) { Direction = ParameterDirection.Output });
            //command.Parameters.Add(new SqlParameter("@FilteredRecords", SqlDbType.Int) { Direction = ParameterDirection.Output });
        }

        private static object MapReaderToData(SqlDataReader reader)
        {
            return new
            {
                AutoId = reader["AutoId"],
                FingerPrintId = reader["FingerPrintId"],
                MachineId = reader["MachineId"],
                Date = ((DateTime)reader["Date"]).ToString("yyyy-MM-dd"),
                Time = ((TimeSpan)reader["Time"]).ToString(@"hh\:mm\:ss"),
                Latitude = reader["Latitude"],
                Longitude = reader["Longitude"],
                HOALR = reader["HOALR"]
            };
        }

        private bool MachineDataExists(int id)
        {
            return _context.HRM_ATD_MachineDatas.Any(e => e.AutoId == id);
        }
        #endregion
    }
}