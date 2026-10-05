const fs = require('fs');

// Appointment.cs
let domainContent = fs.readFileSync('Soluvion.Domain/Models/Appointment.cs', 'utf8');
domainContent = domainContent.replace(
    'public bool MaterialUsageRecorded { get; set; } = false;',
    'public bool MaterialUsageRecorded { get; set; } = false;\n        public string? ExtraMaterials { get; set; } // JSON array of extra materials'
);
fs.writeFileSync('Soluvion.Domain/Models/Appointment.cs', domainContent);

// AppointmentDto.cs
let dtoContent = fs.readFileSync('Soluvion.API/DTOs/AppointmentDtos/AppointmentDto.cs', 'utf8');
dtoContent = dtoContent.replace(
    'public string? CustomerNotes { get; set; }',
    'public string? CustomerNotes { get; set; }\n        public string? ExtraMaterials { get; set; }'
);
fs.writeFileSync('Soluvion.API/DTOs/AppointmentDtos/AppointmentDto.cs', dtoContent);

// CreateAppointmentDto.cs
let createContent = fs.readFileSync('Soluvion.API/DTOs/AppointmentDtos/CreateAppointmentDto.cs', 'utf8');
createContent = createContent.replace(
    'public string? CustomerNotes { get; set; }',
    'public string? CustomerNotes { get; set; }\n        public string? ExtraMaterials { get; set; }'
);
fs.writeFileSync('Soluvion.API/DTOs/AppointmentDtos/CreateAppointmentDto.cs', createContent);

// UpdateAppointmentDto.cs
let updateContent = fs.readFileSync('Soluvion.API/DTOs/AppointmentDtos/UpdateAppointmentDto.cs', 'utf8');
updateContent = updateContent.replace(
    'public string? CustomerNotes { get; set; }',
    'public string? CustomerNotes { get; set; }\n        public string? ExtraMaterials { get; set; }'
);
fs.writeFileSync('Soluvion.API/DTOs/AppointmentDtos/UpdateAppointmentDto.cs', updateContent);

// AppointmentResponseDto.cs
let responseContent = fs.readFileSync('Soluvion.API/DTOs/AppointmentDtos/AppointmentResponseDto.cs', 'utf8');
responseContent = responseContent.replace(
    'public string? CustomerNotes { get; set; }',
    'public string? CustomerNotes { get; set; }\n        public string? ExtraMaterials { get; set; }'
);
fs.writeFileSync('Soluvion.API/DTOs/AppointmentDtos/AppointmentResponseDto.cs', responseContent);
