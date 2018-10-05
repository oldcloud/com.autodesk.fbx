function(stevedore command)
    if(${command} STREQUAL "internal-unpack")
        list(GET ARGN 0 repo_name)
        list(GET ARGN 1 artifact_id)
        list(GET ARGN 2 target_path)
    else()
        message(FATAL_ERROR "Unsupported command `${command}'")
    endif()

    if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin" OR ${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
        find_program(MONO mono)
    else()
        set(MONO "")
    endif()

    find_program(BEE bee.exe HINTS ${CMAKE_SOURCE_DIR})
    if(${BEE} STREQUAL "BEE-NOTFOUND")
        message(FATAL "bee.exe required by Stevedore was not found in ${CMAKE_SOURCE_DIR}")
    endif()

    find_program(7ZA NAMES 7za 7z HINTS "C:/Program Files/7-Zip")
    if(${7ZA} STREQUAL "7ZA-NOTFOUND")
        message(FATAL "7z required by Stevedore was not found")
    endif()
    message(STATUS "Stevedore fetching ${repo_name}:${artifact_id} to ${target_path}")
    file(TO_NATIVE_PATH "${7ZA}" 7ZA_NATIVE)
    # seriously, the only difference between these two is the quotes 
    if(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin" OR ${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
        execute_process(COMMAND ${CMAKE_COMMAND} -E env BEE_INTERNAL_STEVEDORE_7ZA=${7ZA_NATIVE} ${MONO} ${BEE} steve internal-unpack ${repo_name} ${artifact_id} ${target_path})
    else()
        execute_process(COMMAND ${CMAKE_COMMAND} -E env BEE_INTERNAL_STEVEDORE_7ZA="${7ZA_NATIVE}" ${MONO} ${BEE} steve internal-unpack ${repo_name} ${artifact_id} ${target_path})
    endif()
endfunction()
