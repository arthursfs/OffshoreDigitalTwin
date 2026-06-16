function exportMostHybridSparForUnity(output, fileName)
%EXPORTMOSTHYBRIDSPARFORUNITY Export MOST/WEC-Sim HybridSpar output to CSV.
%
% Usage after running the MOST HybridSpar example:
%
%   exportMostHybridSparForUnity(output)
%
% Optional:
%
%   exportMostHybridSparForUnity(output, 'outputs/my_run.csv')
%
% The generated CSV is consumed by the Unity MostPlaybackDriver.

if nargin < 2 || isempty(fileName)
    thisFile = mfilename('fullpath');
    thisDir = fileparts(thisFile);
    fileName = fullfile(thisDir, 'outputs', 'most_unity.csv');
end

outDir = fileparts(fileName);
if ~exist(outDir, 'dir')
    mkdir(outDir);
end

[time, bodyPosition] = readBodyPosition(output);
n = numel(time);

rotorSpeed = nan(n, 1);
turbinePower = nan(n, 1);
genTorque = nan(n, 1);
bladePitch = nan(n, 1);
windSpeed = nan(n, 1);
waveElevation = nan(n, 1);
ptoPower = nan(n, 1);

if isfield(output, 'windTurbine') && ~isempty(output.windTurbine)
    wt = output.windTurbine(1);
    rotorSpeed = resampleColumn(wt, 'rotorSpeed', time, rotorSpeed);
    turbinePower = resampleColumn(wt, 'turbinePower', time, turbinePower);
    genTorque = resampleColumn(wt, 'genTorque', time, genTorque);
    bladePitch = resampleColumn(wt, 'bladePitch', time, bladePitch);
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

if isfield(output, 'wave') && isfield(output.wave, 'elevation') && isfield(output.wave, 'time')
    waveElevation = interp1(output.wave.time(:), output.wave.elevation(:), time, 'linear', 'extrap');
end

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

tableOut = table( ...
    time(:), surge(:), sway(:), heave(:), roll(:), pitch(:), yaw(:), ...
    rotorSpeed(:), rotorAzimuth(:), turbinePower(:), genTorque(:), bladePitch(:), ...
    windSpeed(:), waveElevation(:), ptoPower(:), ...
    'VariableNames', { ...
    'time_s', 'surge_m', 'sway_m', 'heave_m', 'roll_deg', 'pitch_deg', 'yaw_deg', ...
    'rotor_speed_rpm', 'rotor_azimuth_deg', 'turbine_power_mw', 'gen_torque_nm', 'blade_pitch_deg', ...
    'wind_speed_mps', 'wave_elevation_m', 'pto_power_mw'});

writetable(tableOut, fileName);
fprintf('Unity CSV written to %s\n', fileName);
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

function deg = radiansToDegreesIfNeeded(values)
if max(abs(values), [], 'omitnan') < 2*pi
    deg = values * 180 / pi;
else
    deg = values;
end
end

