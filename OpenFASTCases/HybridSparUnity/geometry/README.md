# Geometry

Use this folder for visualization and source CAD files:

- `.STL` files are convenient for Unity visuals after import/conversion.
- `.STEP` files are source geometry for CAD tools, hydrodynamic preprocessing, and traceability.

MOST's HybridSpar example keeps both `.STEP` and `.STL` versions of turbine parts. Follow the same idea here: keep source geometry and Unity-friendly visual geometry together, but treat OpenFAST numerical input files as the authoritative simulation model.

Unity does not natively import every STEP workflow. Convert STEP to FBX, OBJ, or Unity-supported mesh assets when needed.

