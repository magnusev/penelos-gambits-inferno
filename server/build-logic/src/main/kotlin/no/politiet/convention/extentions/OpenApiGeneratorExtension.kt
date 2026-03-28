package no.politiet.convention.extentions

open class OpenApiGeneratorExtension {
    lateinit var inputSpec: String
    lateinit var modelPackage: String
}

data class OpenApiGeneratorSettings(
    val inputSpec: String,
    val modelPackage: String,
)

fun collectSettings(ext: OpenApiGeneratorExtension): OpenApiGeneratorSettings {
    return OpenApiGeneratorSettings(
        inputSpec = ext.inputSpec,
        modelPackage = ext.modelPackage,
    )
}

