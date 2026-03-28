package com.penelosgambits.convention.extentions

open class KtorServiceExtension {
    lateinit var mainClass: String
    lateinit var dockerImageName: String
    lateinit var dockerImageVersion: String
    var dockerPorts: List<String> = listOf("8080")
}

data class KtorServiceSettings(
    val mainClass: String,
    val dockerImageName: String,
    val dockerImageVersion: String,
    val dockerPorts: List<String>
)

fun collectSettings(ext: KtorServiceExtension): KtorServiceSettings {
    return KtorServiceSettings(
        mainClass = ext.mainClass,
        dockerImageName = ext.dockerImageName,
        dockerImageVersion = ext.dockerImageVersion,
        dockerPorts = ext.dockerPorts
    )
}
