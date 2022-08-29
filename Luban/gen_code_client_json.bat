set WORKSPACE=..

set GEN_CLIENT=%WORKSPACE%\Luban\Luban.ClientServer\Luban.ClientServer.exe
set CONF_ROOT=%WORKSPACE%\Luban\Config

%GEN_CLIENT% --template_search_path ComtomTemplate -j cfg --^
 -d %CONF_ROOT%\Defines\__root__.xml ^
 --input_data_dir %CONF_ROOT%\Datas ^
 --output_code_dir %WORKSPACE%/Unity/Assets/GameMain/Scripts/Hotfix/Generate ^
 --output_data_dir %WORKSPACE%\Unity\Assets\GameMain\DataTables\Bin ^
 --gen_types code_template,data_json ^
 --template:code:dir cs_client ^
 -s client 

pause