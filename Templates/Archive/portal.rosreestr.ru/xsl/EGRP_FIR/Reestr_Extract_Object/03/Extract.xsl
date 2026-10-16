<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template name="Extract">
    <xsl:param name="Extract"/>
    <xsl:for-each select="$Extract">
      <table class="t" border="1" cellpadding="2" cellspacing="0" width="100%">
        <tr>
          <td rowspan="9" width="1%">1.</td>
          <td colspan="2" width="31%">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>Кадастровый (или </xsl:text>
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/ConditionalNumber">
                    <u>условный</u>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:text>условный</xsl:text>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:text>) номер объекта:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="BreakPanel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/CadastralNumber">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/CadastralNumber"/>
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/ConditionalNumber"/>
                    </xsl:call-template>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>наименование объекта:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/Name"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>назначение объекта:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/Assignation_Code_Text">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/Assignation_Code_Text"/>
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:choose>
                      <xsl:when test="ObjectRight/ObjectDesc/GroundCategoryText">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/GroundCategoryText"/>
                        </xsl:call-template>
                      </xsl:when>
                      <xsl:otherwise>&#160;</xsl:otherwise>
                    </xsl:choose>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>площадь объекта:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/Area/AreaText">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/Area/AreaText"/>
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>инвентарный номер, литер:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/Inv_No">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/Inv_No"/>
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>этажность (этаж):</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/Floor">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/Floor"/>
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>номера на поэтажном плане:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="BreakPanel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/FloorPlan_No/Explication">
                    <xsl:for-each select="ObjectRight/ObjectDesc/FloorPlan_No/Explication">
                      <xsl:if test="not(position()=1)">
                        <xsl:text>; </xsl:text>
                      </xsl:if>
                      <xsl:call-template name="Value">
                        <xsl:with-param name="Node" select="."/>
                      </xsl:call-template>
                    </xsl:for-each>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>адрес (местоположение) объекта:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="ObjectRight/ObjectDesc/Address/Content"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>состав:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="ObjectRight/ObjectDesc/Complex/Explication">
                    <xsl:for-each select="ObjectRight/ObjectDesc/Complex/Explication">
                      <xsl:if test="not(position()=1)">
                        <xsl:text>; </xsl:text>
                      </xsl:if>
                      <xsl:call-template name="Value">
                        <xsl:with-param name="Node" select="."/>
                      </xsl:call-template>
                    </xsl:for-each>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
		</tr>
        <xsl:call-template name="Rights">
          <xsl:with-param name="Rights" select="ObjectRight/Right" />
        </xsl:call-template>
        <xsl:call-template name="Shares">
          <xsl:with-param name="Shares" select="ObjectRight" />
        </xsl:call-template>
        <tr>
          <td>6.</td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>Правопритязания:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="RightAssert"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td>7.</td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>Заявленные в судебном порядке права требования:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="RightClaim"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
		<tr>
		  <td>8.</td>
		  <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>Дата изменения:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
				<xsl:value-of select="//MdfDate" />
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
      </table>
    </xsl:for-each>
    <p>
      <xsl:text>Выписка выдана: </xsl:text>
      <xsl:call-template name="Value">
        <xsl:with-param name="Node" select="FootContent/Recipient"/>
      </xsl:call-template>
    </p>
  </xsl:template>

  <xsl:template name="Rights">
    <xsl:param name="Rights"/>
    <xsl:for-each select="$Rights">
      <xsl:variable name="RightIndex" select="position()"/>
      <xsl:variable name="Encumbrances" select="count(Encumbrance)"/>
      <tr>
        <td>2.</td>
        <td colspan="2">
          <xsl:call-template name="Panel">
            <xsl:with-param name="Text">
              <xsl:text>Правообладатель (правообладатели):</xsl:text>
            </xsl:with-param>
          </xsl:call-template>
        </td>
        <td width="1%">
          <xsl:text>2.</xsl:text>
          <xsl:value-of select="position()"/>
          <xsl:text>.</xsl:text>
        </td>
        <td>
          <xsl:call-template name="Panel">
            <xsl:with-param name="Text">
              <xsl:choose>
                <xsl:when test="Owner">
                  <xsl:for-each select="Owner">
                    <xsl:if test="not(position()=1)">
                      <xsl:text>;</xsl:text>
                      <br />
                    </xsl:if>
                    <xsl:choose>
                      <xsl:when test="Person">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="Person/Content"/>
                        </xsl:call-template>
                      </xsl:when>
                      <xsl:when test="Organization">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="Organization/Content"/>
                        </xsl:call-template>
                      </xsl:when>
                      <xsl:when test="Governance">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="Governance/Content"/>
                        </xsl:call-template>
                      </xsl:when>
                    </xsl:choose>
                  </xsl:for-each>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:call-template name="Value">
                    <xsl:with-param name="Node" select="NoOwner"/>
                  </xsl:call-template>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:with-param>
          </xsl:call-template>
        </td>
      </tr>
      <tr>
        <td>3.</td>
        <td colspan="2">
          <xsl:call-template name="Panel">
            <xsl:with-param name="Text">
              <xsl:text>Вид, номер и дата государственной регистрации права:</xsl:text>
            </xsl:with-param>
          </xsl:call-template>
        </td>
        <td>
          <xsl:text>3.</xsl:text>
          <xsl:value-of select="position()"/>
          <xsl:text>.</xsl:text>
        </td>
        <td>
          <xsl:call-template name="Panel">
            <xsl:with-param name="Text">
              <xsl:choose>
                <xsl:when test="Registration">
                  <xsl:call-template name="Value">
                    <xsl:with-param name="Node" select="Registration/Name"/>
                  </xsl:call-template>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:call-template name="Value">
                    <xsl:with-param name="Node" select="NoRegistration"/>
                  </xsl:call-template>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:with-param>
          </xsl:call-template>
        </td>
      </tr>
      <tr>
        <td rowspan="{$Encumbrances*6+1}">4.</td>
        <td colspan="2">
          <xsl:call-template name="Panel">
            <xsl:with-param name="Text">
              <xsl:text>Ограничение (обременение) права:</xsl:text>
            </xsl:with-param>
          </xsl:call-template>
        </td>
        <td colspan="2">
          <xsl:call-template name="Panel">
            <xsl:with-param name="Text">
              <xsl:choose>
                <xsl:when test="Encumbrance">&#160;</xsl:when>
                <xsl:otherwise>
                  <xsl:call-template name="Value">
                    <xsl:with-param name="Node" select="NoEncumbrance"/>
                  </xsl:call-template>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:with-param>
          </xsl:call-template>
        </td>
      </tr>
      <xsl:for-each select="Encumbrance">
        <tr>
          <td rowspan="6" width="1%">
            <xsl:text>4.</xsl:text>
            <xsl:value-of select="$RightIndex"/>
            <xsl:text>.</xsl:text>
            <xsl:value-of select="position()"/>
            <xsl:text>.</xsl:text>
          </td>
          <td>
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>вид:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="BreakPanel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="Name"/>
                </xsl:call-template>
                <xsl:if test="ShareText">
                  <xsl:text>, </xsl:text>
                  <xsl:call-template name="Value">
                    <xsl:with-param name="Node" select="ShareText"/>
                  </xsl:call-template>
                </xsl:if>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td>
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>дата государственной регистрации:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="RegDate"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td>
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>номер государственной регистрации:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="BreakPanel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="RegNumber"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td>
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>срок, на который установлено ограничение (обременение) права:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="Duration/Term">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="Duration/Term"/>
                    </xsl:call-template>
                  </xsl:when>
                  <xsl:when test="Duration/Started|Duration/Stopped">
                    <xsl:if test="Duration/Started">
                      <xsl:text>с </xsl:text>
                      <xsl:call-template name="Value">
                        <xsl:with-param name="Node" select="Duration/Started"/>
                      </xsl:call-template>
                    </xsl:if>
                    <xsl:if test="Duration/Stopped">
                      <xsl:text> по </xsl:text>
                      <xsl:call-template name="Value">
                        <xsl:with-param name="Node" select="Duration/Stopped"/>
                      </xsl:call-template>
                    </xsl:if>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td>
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>лицо, в пользу которого установлено ограничение (обременение) права:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="Owner">
                    <xsl:for-each select="Owner">
                      <xsl:if test="not(position()=1)">
                        <xsl:text>;</xsl:text>
                        <br />
                      </xsl:if>
                      <xsl:choose>
                        <xsl:when test="Person">
                          <xsl:call-template name="Value">
                            <xsl:with-param name="Node" select="Person/Content"/>
                          </xsl:call-template>
                        </xsl:when>
                        <xsl:when test="Organization">
                          <xsl:call-template name="Value">
                            <xsl:with-param name="Node" select="Organization/Content"/>
                          </xsl:call-template>
                        </xsl:when>
                        <xsl:when test="Governance">
                          <xsl:call-template name="Value">
                            <xsl:with-param name="Node" select="Governance/Content"/>
                          </xsl:call-template>
                        </xsl:when>
                      </xsl:choose>
                    </xsl:for-each>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="AllShareOwner"/>
                    </xsl:call-template>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
        <tr>
          <td>
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>основание государственной регистрации:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:choose>
                  <xsl:when test="DocFound">
                    <xsl:for-each select="DocFound">
                      <xsl:if test="not(position()=1)">
                        <xsl:text>;</xsl:text>
                        <br />
                      </xsl:if>
                      <xsl:call-template name="Value">
                        <xsl:with-param name="Node" select="Content"/>
                      </xsl:call-template>
                    </xsl:for-each>
                  </xsl:when>
                  <xsl:otherwise>&#160;</xsl:otherwise>
                </xsl:choose>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
      </xsl:for-each>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name="Shares">
    <xsl:param name="Shares"/>
    <xsl:choose>
      <xsl:when test="count($Shares/ShareHolding) > 0">
        <tr>
          <td rowspan="{count($Shares/ShareHolding)+count($Shares/ShareHolding/Owner)+count($Shares/ShareHolding/Encumbrance)+1}">5.</td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>Договоры участия в долевом строительстве:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">&#160;</td>
        </tr>
        <xsl:for-each select="$Shares/ShareHolding">
          <xsl:variable name="Owners" select="count(Owner)" />
          <xsl:variable name="Encumbrances" select="count(Encumbrance)" />
          <tr>
            <td rowspan="{$Owners+$Encumbrances+1}">
              <xsl:text>5.</xsl:text>
              <xsl:value-of select="position()"/>
              <xsl:text>.</xsl:text>
            </td>
            <td>
              <xsl:call-template name="Panel">
                <xsl:with-param name="Text">
                  <xsl:text>объект долевого строительства:</xsl:text>
                </xsl:with-param>
              </xsl:call-template>
            </td>
            <td colspan="2">
              <xsl:call-template name="Panel">
                <xsl:with-param name="Text">
                  <xsl:choose>
                    <xsl:when test="ShareHolding/ShareObjects">
                      <xsl:for-each select="ShareHolding/ShareObjects">
                        <xsl:if test="not(position()=1)">
                          <xsl:text>;</xsl:text>
                          <br />
                        </xsl:if>
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="."/>
                        </xsl:call-template>
                      </xsl:for-each>
                    </xsl:when>
                    <xsl:otherwise>&#160;</xsl:otherwise>
                  </xsl:choose>
                </xsl:with-param>
              </xsl:call-template>
            </td>
          </tr>
          <xsl:for-each select="Owner">
            <tr>
              <xsl:if test="position()=1">
                <td rowspan="{$Owners+$Encumbrances}">
                  <xsl:call-template name="Panel">
                    <xsl:with-param name="Text">
                      <xsl:text>участники долевого строительства:</xsl:text>
                    </xsl:with-param>
                  </xsl:call-template>
                </td>
              </xsl:if>
              <td colspan="2">
                <xsl:call-template name="Panel">
                  <xsl:with-param name="Text">
                    <xsl:choose>
                      <xsl:when test="Person">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="Person/Content"/>
                        </xsl:call-template>
                      </xsl:when>
                      <xsl:when test="Organization">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="Organization/Content"/>
                        </xsl:call-template>
                      </xsl:when>
                      <xsl:when test="Governance">
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="Governance/Content"/>
                        </xsl:call-template>
                      </xsl:when>
                    </xsl:choose>
                  </xsl:with-param>
                </xsl:call-template>
              </td>
            </tr>
          </xsl:for-each>
          <xsl:for-each select="Encumbrance">
            <tr>
              <td colspan="2">
                <xsl:call-template name="Panel">
                  <xsl:with-param name="Text">
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="Name"/>
                    </xsl:call-template>
                    <xsl:text>;</xsl:text>
                    <br/>
                    <xsl:text>номер государственной регистрации: </xsl:text>
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="RegNumber"/>
                    </xsl:call-template>
                    <xsl:text>;</xsl:text>
                    <br/>
                    <xsl:text>дата государственной регистрации: </xsl:text>
                    <xsl:call-template name="Value">
                      <xsl:with-param name="Node" select="RegDate"/>
                    </xsl:call-template>
                    <xsl:text>;</xsl:text>
                    <xsl:if test="Duration/Started|Duration/Stopped">
                      <br/>
                      <xsl:text>срок, на который установлено ограничение (обременение) права: </xsl:text>
                      <xsl:choose>
                        <xsl:when test="Duration/Term">
                          <xsl:call-template name="Value">
                            <xsl:with-param name="Node" select="Duration/Term"/>
                          </xsl:call-template>
                        </xsl:when>
                        <xsl:when test="Duration/Started|Duration/Stopped">
                          <xsl:if test="Duration/Started">
                            <xsl:text>с </xsl:text>
                            <xsl:call-template name="Value">
                              <xsl:with-param name="Node" select="Duration/Started"/>
                            </xsl:call-template>
                          </xsl:if>
                          <xsl:if test="Duration/Stopped">
                            <xsl:text> по </xsl:text>
                            <xsl:call-template name="Value">
                              <xsl:with-param name="Node" select="Duration/Stopped"/>
                            </xsl:call-template>
                          </xsl:if>
                        </xsl:when>
                        <xsl:otherwise>&#160;</xsl:otherwise>
                      </xsl:choose>
                    </xsl:if>
                    <xsl:choose>
                      <xsl:when test="count(Owner) > 0">
                        <xsl:for-each select="Owner">
                          <xsl:text>;</xsl:text>
                          <br />
                          <xsl:choose>
                            <xsl:when test="Person">
                              <xsl:call-template name="Value">
                                <xsl:with-param name="Node" select="Person/Content"/>
                              </xsl:call-template>
                            </xsl:when>
                            <xsl:when test="Organization">
                              <xsl:call-template name="Value">
                                <xsl:with-param name="Node" select="Organization/Content"/>
                              </xsl:call-template>
                            </xsl:when>
                            <xsl:when test="Governance">
                              <xsl:call-template name="Value">
                                <xsl:with-param name="Node" select="Governance/Content"/>
                              </xsl:call-template>
                            </xsl:when>
                          </xsl:choose>
                        </xsl:for-each>
                      </xsl:when>
                      <xsl:otherwise>
                        <xsl:text>;</xsl:text>
                        <br />
                        <xsl:call-template name="Value">
                          <xsl:with-param name="Node" select="AllShareOwner"/>
                        </xsl:call-template>
                      </xsl:otherwise>
                    </xsl:choose>
                  </xsl:with-param>
                </xsl:call-template>
              </td>
            </tr>
          </xsl:for-each>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <tr>
          <td>5.</td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:text>Договоры участия в долевом строительстве:</xsl:text>
              </xsl:with-param>
            </xsl:call-template>
          </td>
          <td colspan="2">
            <xsl:call-template name="Panel">
              <xsl:with-param name="Text">
                <xsl:call-template name="Value">
                  <xsl:with-param name="Node" select="$Shares/NoShareHolding"/>
                </xsl:call-template>
              </xsl:with-param>
            </xsl:call-template>
          </td>
        </tr>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
