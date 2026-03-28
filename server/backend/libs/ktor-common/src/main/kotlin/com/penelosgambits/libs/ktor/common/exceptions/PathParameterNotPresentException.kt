package com.penelosgambits.libs.ktor.common.exceptions

class PathParameterNotPresentException(
    val fieldName: String,
    override val message: String = "Path parameter $fieldName is missing"
) : RuntimeException()
