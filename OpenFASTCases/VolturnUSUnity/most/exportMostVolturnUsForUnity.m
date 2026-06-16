function exportMostVolturnUsForUnity(output, unityCsvFile, comparisonCsvFile, fastData)
%EXPORTMOSTVOLTURNUSFORUNITY Export MOST VolturnUS output for Unity playback.
%
% Basic usage after running the MOST VolturnUS example:
%
%   exportMostVolturnUsForUnity(output)
%
% Optional MOST/FAST comparison CSV:
%
%   fastData = readtable('../openfast/volturn_fast_channels.csv');
%   exportMostVolturnUsForUnity(output, ...
%       'outputs/volturn_most_unity.csv', ...
%       'outputs/volturn_most_fast_comparison.csv', ...
%       fastData)
%
% The Unity scene builder consumes the playback CSV. The comparison HUD
% consumes the merged MOST/FAST CSV.

if nargin < 2 || isempty(unityCsvFile)
    thisFile = mfilename('fullpath');
    thisDir = fileparts(thisFile);
    unityCsvFile = fullfile(thisDir, 'outputs', 'volturn_most_unity.csv');
end

if nargin < 3 || isempty(comparisonCsvFile)
    [outDir, ~, ~] = fileparts(unityCsvFile);
    comparisonCsvFile = fullfile(outDir, 'volturn_most_fast_comparison.csv');
end

if nargin < 4
    fastData = [];
end

[time, bodyPosition] = readBodyPosition(output);
n = numel(time);

rotorSpeed = nan(n, 1);
turbinePower = nan(n, 1);
genTorque = nan(n, 1);
bladePitch = nan(n, 1);
windSpeed = nan(n, 1);
rootAxialForce = nan(n, 1);
rootAxialMoment = nan(n, 1);

if isfield(output, 'windTurbine') && ~isempty(output.windTurbine)
    wt = output.windTurbine(1);
    rotorSpeed = resampleColumn(wt, 'rotorSpeed', time, rotorSpeed);
    turbinePower = resampleColumn(wt, 'turbinePower', time, turbinePower);
    genTorque = resampleColumn(wt, 'genTorque', time, genTorque);
    bladePitch = resampleColumn(wt, 'bladePitch', time, bladePitch);
    rootAxialForce = resampleFirstAvailable(wt, {'rootAxialForce', 'bladeRootAxialForce', 'RootFxb1'}, time, rootAxialForce);
    rootAxialMoment = resampleFirstAvailable(wt, {'rootAxialMoment', 'bladeRootAxialMoment', 'RootMxb1'}, time, rootAxialMoment);

    if isfield(wt, 'windSpeed') && isfield(wt, 'time')
        ws = wt.windSpeed;
        if size(ws, 2) >= 3
            windModule = sqrt(sum(ws(:, 1:3).^2, 2));
        else
            windModule = ws(:, 1);
        end
        windSpeed = interp1(wt.time(:), windModule(:), time, 'linear', 'extrap');
    end
end

waveElevation = nan(n, 1);
if isfield(output, 'wave') && isfield(output.wave, 'elevation') && isfield(output.wave, 'time')
    waveElevation = interp1(output.wave.time(:), output.wave.elevation(:), time, 'linear', 'extrap');
end

ptoPower = nan(n, 1);
if isfield(output, 'pto') && ~isempty(output.pto)
    pto = output.pto(1);
    if isfield(pto, 'powerInternalMechanics') && isfield(pto, 'time')
        ptoPower = interp1(pto.time(:), pto.powerInternalMechanics(:), time, 'linear', 'extrap');
    elseif isfield(pto, 'power') && isfield(pto, 'time')
        ptoPower = interp1(pto.time(:), pto.power(:), time, 'linear', 'extrap');
    end
end

surge = bodyPosition(:, 1);
sway = bodyPosition(:, 2);
heave = bodyPosition(:, 3);
roll = radiansToDegreesIfNeeded(bodyPosition(:, 4));
pitch = radiansToDegreesIfNeeded(bodyPosition(:, 5));
yaw = radiansToDegreesIfNeeded(bodyPosition(:, 6));

rotorAzimuth = cumtrapz(time, rotorSpeed * 6);
rotorAzimuth = mod(rotorAzimuth, 360);

turbinePowerMw = normalizePowerMw(turbinePower);
rootAxialForceKn = normalizeForceKn(rootAxialForce);
rootAxialMomentMnm = normalizeMomentMnm(rootAxialMoment);

playbackTable = table( ...
    time(:), surge(:), sway(:), heave(:), roll(:), pitch(:), yaw(:), ...
    rotorSpeed(:), rotorAzimuth(:), turbinePowerMw(:), genTorque(:), bladePitch(:), ...
    windSpeed(:), waveElevation(:), ptoPower(:), ...
    'VariableNames', { ...
    'time_s', 'surge_m', 'sway_m', 'heave_m', 'roll_deg', 'pitch_deg', 'yaw_deg', ...
    'rotor_speed_rpm', 'rotor_azimuth_deg', 'turbine_power_mw', 'gen_torque_nm', 'blade_pitch_deg', ...
    'wind_speed_mps', 'wave_elevation_m', 'pto_power_mw'});

writeTableCreatingDirectory(playbackTable, unityCsvFile);
fprintf('Unity playback CSV written to %s\n', unityCsvFile);

comparisonTable = table( ...
    time(:), pitch(:), nan(n, 1), turbinePowerMw(:), nan(n, 1), ...
    rootAxialForceKn(:), nan(n, 1), rootAxialMomentMnm(:), nan(n, 1), ...
    'VariableNames', { ...
    'time_s', ...
    'most_pitch_deg', 'fast_pitch_deg', ...
    'most_power_mw', 'fast_power_mw', ...
    'most_root_axial_force_kn', 'fast_root_axial_force_kn', ...
    'most_root_axial_moment_mnm', 'fast_root_axial_moment_mnm'});

if ~isempty(fastData)
    comparisonTable.fast_pitch_deg = resampleExternal(fastData, {'fast_pitch_deg', 'PtfmPitch', 'pitch_deg'}, time, comparisonTable.fast_pitch_deg);
    comparisonTable.fast_power_mw = normalizePowerMw(resampleExternal(fastData, {'fast_power_mw', 'GenPwr', 'power_mw'}, time, comparisonTable.fast_power_mw));
    comparisonTable.fast_root_axial_force_kn = normalizeForceKn(resampleExternal(fastData, {'fast_root_axial_force_kn', 'RootFxb1', 'rootAxialForce'}, time, comparisonTable.fast_root_axial_force_kn));
    comparisonTable.fast_root_axial_moment_mnm = normalizeMomentMnm(resampleExternal(fastData, {'fast_root_axial_moment_mnm', 'RootMxb1', 'rootAxialMoment'}, time, comparisonTable.fast_root_axial_moment_mnm));
end

writeTableCreatingDirectory(comparisonTable, comparisonCsvFile);
fprintf('MOST/FAST comparison CSV written to %s\n', comparisonCsvFile);
end

function [time, position] = readBodyPosition(output)
if ~isfield(output, 'bodies') || isempty(output.bodies)
    error('Expected output.bodies from MOST/WEC-Sim.');
end

body = output.bodies(1);
if ~isfield(body, 'time') || ~isfield(body, 'position')
    error('Expected output.bodies(1).time and output.bodies(1).position.');
end

time = body.time(:);
position = body.position;
if size(position, 2) < 6
    error('Expected body.position columns: surge sway heave roll pitch yaw.');
end
position = position(:, 1:6);
end

function values = resampleColumn(structure, fieldName, targetTime, fallback)
values = fallback;
if isfield(structure, fieldName) && isfield(structure, 'time')
    raw = structure.(fieldName);
    if size(raw, 2) > 1
        raw = raw(:, 1);
    end
    values = interp1(structure.time(:), raw(:), targetTime, 'linear', 'extrap');
end
end

function values = resampleFirstAvailable(structure, fieldNames, targetTime, fallback)
values = fallback;
for i = 1:numel(fieldNames)
    candidate = resampleColumn(structure, fieldNames{i}, targetTime, []);
    if ~isempty(candidate)
        values = candidate;
        return;
    end
end
end

function values = resampleExternal(data, fieldNames, targetTime, fallback)
values = fallback;
if istable(data)
    names = data.Properties.VariableNames;
    time = firstTableColumn(data, {'time_s', 'Time', 'time'});
    if isempty(time)
        return;
    end

    for i = 1:numel(fieldNames)
        match = strcmpi(names, fieldNames{i});
        if any(match)
            raw = data{:, find(match, 1)};
            values = interp1(time(:), raw(:), targetTime, 'linear', 'extrap');
            return;
        end
    end
elseif isstruct(data)
    if isfield(data, 'time_s')
        time = data.time_s;
    elseif isfield(data, 'Time')
        time = data.Time;
    elseif isfield(data, 'time')
        time = data.time;
    else
        return;
    end

    for i = 1:numel(fieldNames)
        if isfield(data, fieldNames{i})
            raw = data.(fieldNames{i});
            values = interp1(time(:), raw(:), targetTime, 'linear', 'extrap');
            return;
        end
    end
end
end

function column = firstTableColumn(data, fieldNames)
column = [];
names = data.Properties.VariableNames;
for i = 1:numel(fieldNames)
    match = strcmpi(names, fieldNames{i});
    if any(match)
        column = data{:, find(match, 1)};
        return;
    end
end
end

function values = normalizePowerMw(values)
if max(abs(values), [], 'omitnan') > 1000
    values = values / 1000;
end
end

function values = normalizeForceKn(values)
if max(abs(values), [], 'omitnan') > 10000
    values = values / 1000;
end
end

function values = normalizeMomentMnm(values)
if max(abs(values), [], 'omitnan') > 10000
    values = values / 1e6;
end
end

function deg = radiansToDegreesIfNeeded(values)
if max(abs(values), [], 'omitnan') < 2*pi
    deg = values * 180 / pi;
else
    deg = values;
end
end

function writeTableCreatingDirectory(tableOut, fileName)
outDir = fileparts(fileName);
if ~exist(outDir, 'dir')
    mkdir(outDir);
end
writetable(tableOut, fileName);
end
