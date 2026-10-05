const fs = require('fs');

const updateFile = (path, target, addition) => {
    try {
        let content = fs.readFileSync(path, 'utf8');
        if (!content.includes('ExtraMaterials')) {
            content = content.replace(target, target + '\n' + addition);
            fs.writeFileSync(path, content, 'utf8');
            console.log('Updated ' + path);
        }
    } catch(e) {}
};

updateFile('Soluvion.API/DTOs/AppointmentDtos/CreateAppointmentDto.cs', 'public string? CustomerNotes { get; set; }', '        public string? ExtraMaterials { get; set; }');
updateFile('Soluvion.API/DTOs/AppointmentDtos/UpdateAppointmentDto.cs', 'public string? CustomerNotes { get; set; }', '        public string? ExtraMaterials { get; set; }');
updateFile('Soluvion.API/DTOs/AppointmentDtos/AppointmentResponseDto.cs', 'public string? CustomerNotes { get; set; }', '        public string? ExtraMaterials { get; set; }');
