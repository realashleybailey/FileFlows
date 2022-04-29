
@echo off
REM here we specify the root directory is two paths up, the source FileFlows dir, but the Dockerfile is the one in this directory
CALL :NORMALIZEPATH "..\.."
SET SRCPATH=%RETVAL%

@echo on
docker build %SRCPATH% -f ./Dockerfile --tag fileflows:build
:: docker run --rm --name FileFlowsBuild -v %SRCPATH%:/src fileflows:build
docker run --rm --name FileFlowsBuild -v %SRCPATH%:/src fileflows:build --target Server


:: ========== FUNCTIONS ==========
@echo off
EXIT /B

:NORMALIZEPATH
  SET RETVAL=%~f1
  EXIT /B