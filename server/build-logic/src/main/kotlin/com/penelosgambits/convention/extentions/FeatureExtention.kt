package com.penelosgambits.convention.extentions

open class FeatureExtention {
    var name: String? = null
}

data class FeatureSettings(
    val name: String?
)

fun collectSettings(ext: FeatureExtention): FeatureSettings {
    return FeatureSettings(
        name = ext.name
    )
}
